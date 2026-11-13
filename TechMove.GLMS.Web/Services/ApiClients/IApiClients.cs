using Microsoft.AspNetCore.Http;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Services.ApiClients
{
    public interface IAuthApiClient
    {
        Task<ApiAuthResponse?> LoginAsync(string username, string password);
        string? GetActiveToken();
        void SetActiveToken(string token, string username, string role);
        void Logout();
        Task<string> EnsureAuthenticatedTokenAsync();
    }

    public interface IContractApiClient
    {
        Task<List<ApiContractDto>> GetContractsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            ContractStatus? status = null,
            int? clientId = null,
            string? searchTerm = null);

        Task<ApiContractDto?> GetContractByIdAsync(int id);

        Task<ApiContractDto> CreateContractAsync(ApiCreateContractRequest request, IFormFile? agreementPdf = null);

        Task<ApiContractDto> UpdateStatusAsync(int id, ContractStatus newStatus, string reason);

        Task<ApiContractDto> UploadAgreementAsync(int id, IFormFile file);

        Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadAgreementAsync(int id);
    }

    public interface IClientApiClient
    {
        Task<List<ApiClientDto>> GetClientsAsync();
        Task<ApiClientDto?> GetClientByIdAsync(int id);
    }

    public interface IServiceRequestApiClient
    {
        Task<List<ApiServiceRequestDto>> GetAllRequestsAsync();
        Task<ApiServiceRequestDto?> GetRequestByIdAsync(int id);
        Task<List<ApiServiceRequestDto>> GetRequestsByContractAsync(int contractId);
        Task<ApiServiceRequestDto> CreateServiceRequestAsync(ApiCreateServiceRequestRequest request);
    }

    public interface ICurrencyApiClient
    {
        Task<ApiCurrencyConversionResult> ConvertCurrencyAsync(decimal amount, string currency = "USD");
        Task<decimal> GetExchangeRateAsync(string from = "USD", string to = "ZAR");
    }
}

