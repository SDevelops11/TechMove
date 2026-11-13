using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TechMove.GLMS.API.DTOs;
using Xunit;

namespace TechMove.GLMS.Tests.IntegrationTests
{
    public class AuthAndCurrencyIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AuthAndCurrencyIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_WithValidCredentials_Returns200Ok_AndValidJwtBearerToken()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "admin@techmove.com",
                Password = "Admin@123"
            };

            // Act: POST /api/auth/login
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert: 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
            Assert.NotNull(auth);
            Assert.False(string.IsNullOrWhiteSpace(auth.Token), "JWT token must not be null or empty.");
            Assert.Equal("admin@techmove.com", auth.Username);
            Assert.Equal("Admin", auth.Role);
            Assert.True(auth.Expiration > DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
        {
            // Arrange
            var invalidLogin = new LoginRequestDto
            {
                Username = "unknown_user@random.com",
                Password = "WrongPassword999!"
            };

            // Act: POST /api/auth/login
            var response = await _client.PostAsJsonAsync("/api/auth/login", invalidLogin);

            // Assert: 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUser_WithoutBearerToken_Returns401Unauthorized()
        {
            // Act: GET /api/auth/me without Authorization header
            var response = await _client.GetAsync("/api/auth/me");

            // Assert: 401 Unauthorized
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUser_WithValidBearerToken_Returns200Ok_AndUserClaims()
        {
            // Step 1: Login to get token
            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Username = "manager@techmove.com",
                Password = "Manager@123"
            });
            var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>(_jsonOptions);
            Assert.NotNull(auth);

            // Step 2: Call protected /api/auth/me with Bearer token
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

            var response = await _client.SendAsync(request);

            // Assert: 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);

            Assert.NotNull(user);
            Assert.Equal("manager@techmove.com", user.Username);
            Assert.Equal("LogisticsManager", user.Role);
        }

        [Fact]
        public async Task CurrencyConvert_WithValidAmount_Returns200Ok_AndCalculatedRate()
        {
            // Act: GET /api/currency/convert?amount=1000&currency=USD
            var response = await _client.GetAsync("/api/currency/convert?amount=1000&currency=USD");

            // Assert: 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<CurrencyConversionResultDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal(1000m, result.OriginalAmount);
            Assert.Equal("USD", result.FromCurrency);
            Assert.Equal("ZAR", result.ToCurrency);
            Assert.True(result.ExchangeRate > 0);
            Assert.True(result.ConvertedAmount > 0);
            Assert.False(string.IsNullOrWhiteSpace(result.StrategyUsed));
        }

        [Fact]
        public async Task CurrencyConvert_WithNegativeAmount_Returns400BadRequest()
        {
            // Act: Negative amount
            var response = await _client.GetAsync("/api/currency/convert?amount=-100&currency=USD");

            // Assert: 400 Bad Request
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetExchangeRate_Returns200Ok_AndPositiveSpotRate()
        {
            // Act: GET /api/currency/rate?from=USD&to=ZAR
            var response = await _client.GetAsync("/api/currency/rate?from=USD&to=ZAR");

            // Assert: 200 OK
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<ExchangeRateDto>(_jsonOptions);
            Assert.NotNull(result);
            Assert.Equal("USD", result.FromCurrency);
            Assert.Equal("ZAR", result.ToCurrency);
            Assert.True(result.Rate > 0);
        }
    }
}

