using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Services.ApiClients
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthApiClient> _logger;
        private static string? _cachedToken;
        private static DateTime _tokenExpiration = DateTime.MinValue;
        private static string? _currentUsername;
        private static string? _currentRole;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ApiAuthResponse?> LoginAsync(string username, string password)
        {
            try
            {
                var content = new StringContent(
                    JsonSerializer.Serialize(new ApiLoginRequest { Username = username, Password = password }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("/api/auth/login", content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API Login failed with status {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiAuthResponse>(json, _jsonOptions);
                if (result != null)
                {
                    SetActiveToken(result.Token, result.Username, result.Role);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during API login call");
                return null;
            }
        }

        public string? GetActiveToken()
        {
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiration.AddMinutes(-2))
            {
                return _cachedToken;
            }
            return null;
        }

        public void SetActiveToken(string token, string username, string role)
        {
            _cachedToken = token;
            _tokenExpiration = DateTime.UtcNow.AddMinutes(115);
            _currentUsername = username;
            _currentRole = role;
        }

        public void Logout()
        {
            _cachedToken = null;
            _tokenExpiration = DateTime.MinValue;
            _currentUsername = null;
            _currentRole = null;
        }

        public async Task<string> EnsureAuthenticatedTokenAsync()
        {
            var active = GetActiveToken();
            if (!string.IsNullOrEmpty(active))
            {
                return active;
            }

            // Authenticate with standard enterprise demo account
            var loginResult = await LoginAsync("admin@techmove.com", "Admin@123");
            return loginResult?.Token ?? string.Empty;
        }
    }

    public class ContractApiClient : IContractApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthApiClient _authClient;
        private readonly ILogger<ContractApiClient> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ContractApiClient(
            HttpClient httpClient,
            IAuthApiClient authClient,
            ILogger<ContractApiClient> logger)
        {
            _httpClient = httpClient;
            _authClient = authClient;
            _logger = logger;
        }

        private async Task PrepareAuthHeaderAsync()
        {
            var token = await _authClient.EnsureAuthenticatedTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<ApiContractDto>> GetContractsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            ContractStatus? status = null,
            int? clientId = null,
            string? searchTerm = null)
        {
            await PrepareAuthHeaderAsync();

            var queryParams = new List<string>();
            if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            if (status.HasValue) queryParams.Add($"status={status.Value}");
            if (clientId.HasValue) queryParams.Add($"clientId={clientId.Value}");
            if (!string.IsNullOrWhiteSpace(searchTerm)) queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

            var url = "/api/contracts" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
            var response = await _httpClient.GetAsync(url);
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ApiContractDto>>(json, _jsonOptions) ?? new List<ApiContractDto>();
        }

        public async Task<ApiContractDto?> GetContractByIdAsync(int id)
        {
            await PrepareAuthHeaderAsync();

            var response = await _httpClient.GetAsync($"/api/contracts/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiContractDto>(json, _jsonOptions);
        }

        public async Task<ApiContractDto> CreateContractAsync(ApiCreateContractRequest request, IFormFile? agreementPdf = null)
        {
            await PrepareAuthHeaderAsync();

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/contracts", content);
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            var createdContract = JsonSerializer.Deserialize<ApiContractDto>(json, _jsonOptions)
                ?? throw new ApiException(response.StatusCode, "Failed to parse created contract response from API.");

            // Upload PDF if attached
            if (agreementPdf != null && agreementPdf.Length > 0)
            {
                createdContract = await UploadAgreementAsync(createdContract.Id, agreementPdf);
            }

            return createdContract;
        }

        public async Task<ApiContractDto> UpdateStatusAsync(int id, ContractStatus newStatus, string reason)
        {
            await PrepareAuthHeaderAsync();

            var body = new ApiUpdateContractStatusRequest
            {
                NewStatus = newStatus,
                Reason = reason
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync($"/api/contracts/{id}/status", content);
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiContractDto>(json, _jsonOptions)
                ?? throw new ApiException(response.StatusCode, "Failed to parse updated contract response from API.");
        }

        public async Task<ApiContractDto> UploadAgreementAsync(int id, IFormFile file)
        {
            await PrepareAuthHeaderAsync();

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/pdf");
            content.Add(streamContent, "file", file.FileName);

            var response = await _httpClient.PostAsync($"/api/contracts/{id}/agreement", content);
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiContractDto>(json, _jsonOptions)
                ?? throw new ApiException(response.StatusCode, "Failed to parse agreement upload response.");
        }

        public async Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadAgreementAsync(int id)
        {
            await PrepareAuthHeaderAsync();

            var response = await _httpClient.GetAsync($"/api/contracts/{id}/agreement");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            await EnsureSuccessOrThrowAsync(response);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? $"Contract_{id}_Agreement.pdf";

            fileName = fileName.Trim('"');
            return (bytes, contentType, fileName);
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            var rawBody = await response.Content.ReadAsStringAsync();
            string message = $"API request failed with status {response.StatusCode} ({(int)response.StatusCode})";

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    message = msgProp.GetString() ?? message;
                }
                else if (doc.RootElement.TryGetProperty("title", out var titleProp))
                {
                    message = titleProp.GetString() ?? message;
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    message = rawBody;
                }
            }

            throw new ApiException(response.StatusCode, message, rawBody);
        }
    }

    public class ClientApiClient : IClientApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthApiClient _authClient;
        private readonly ILogger<ClientApiClient> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ClientApiClient(HttpClient httpClient, IAuthApiClient authClient, ILogger<ClientApiClient> logger)
        {
            _httpClient = httpClient;
            _authClient = authClient;
            _logger = logger;
        }

        public async Task<List<ApiClientDto>> GetClientsAsync()
        {
            var token = await _authClient.EnsureAuthenticatedTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.GetAsync("/api/clients");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch clients from API: {StatusCode}", response.StatusCode);
                return new List<ApiClientDto>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ApiClientDto>>(json, _jsonOptions) ?? new List<ApiClientDto>();
        }

        public async Task<ApiClientDto?> GetClientByIdAsync(int id)
        {
            var token = await _authClient.EnsureAuthenticatedTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.GetAsync($"/api/clients/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiClientDto>(json, _jsonOptions);
        }
    }

    public class ServiceRequestApiClient : IServiceRequestApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthApiClient _authClient;
        private readonly ILogger<ServiceRequestApiClient> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ServiceRequestApiClient(
            HttpClient httpClient,
            IAuthApiClient authClient,
            ILogger<ServiceRequestApiClient> logger)
        {
            _httpClient = httpClient;
            _authClient = authClient;
            _logger = logger;
        }

        private async Task PrepareAuthHeaderAsync()
        {
            var token = await _authClient.EnsureAuthenticatedTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<ApiServiceRequestDto>> GetAllRequestsAsync()
        {
            await PrepareAuthHeaderAsync();
            var response = await _httpClient.GetAsync("/api/servicerequests");
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ApiServiceRequestDto>>(json, _jsonOptions) ?? new List<ApiServiceRequestDto>();
        }

        public async Task<ApiServiceRequestDto?> GetRequestByIdAsync(int id)
        {
            await PrepareAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/servicerequests/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiServiceRequestDto>(json, _jsonOptions);
        }

        public async Task<List<ApiServiceRequestDto>> GetRequestsByContractAsync(int contractId)
        {
            await PrepareAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/servicerequests/by-contract/{contractId}");
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ApiServiceRequestDto>>(json, _jsonOptions) ?? new List<ApiServiceRequestDto>();
        }

        public async Task<ApiServiceRequestDto> CreateServiceRequestAsync(ApiCreateServiceRequestRequest request)
        {
            await PrepareAuthHeaderAsync();
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/servicerequests", content);
            await EnsureSuccessOrThrowAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiServiceRequestDto>(json, _jsonOptions)
                ?? throw new ApiException(response.StatusCode, "Failed to parse created service request response.");
        }

        private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            var rawBody = await response.Content.ReadAsStringAsync();
            string message = $"API request failed with status {response.StatusCode}";

            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    message = msgProp.GetString() ?? message;
                }
                else if (doc.RootElement.TryGetProperty("title", out var titleProp))
                {
                    message = titleProp.GetString() ?? message;
                }
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    message = rawBody;
                }
            }

            throw new ApiException(response.StatusCode, message, rawBody);
        }
    }

    public class CurrencyApiClient : ICurrencyApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyApiClient> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CurrencyApiClient(HttpClient httpClient, ILogger<CurrencyApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ApiCurrencyConversionResult> ConvertCurrencyAsync(decimal amount, string currency = "USD")
        {
            var response = await _httpClient.GetAsync($"/api/currency/convert?amount={amount}&currency={Uri.EscapeDataString(currency)}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiCurrencyConversionResult>(json, _jsonOptions)
                ?? new ApiCurrencyConversionResult { OriginalAmount = amount, ConvertedAmount = amount * 18.5m };
        }

        public async Task<decimal> GetExchangeRateAsync(string from = "USD", string to = "ZAR")
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/currency/rate?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ApiExchangeRateResult>(json, _jsonOptions);
                    if (result != null && result.Rate > 0)
                    {
                        return result.Rate;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch exchange rate from API, using default fallback rate");
            }

            return from.ToUpperInvariant() switch
            {
                "USD" => 18.50m,
                "EUR" => 19.80m,
                "GBP" => 23.20m,
                _ => 1.00m
            };
        }
    }
}

