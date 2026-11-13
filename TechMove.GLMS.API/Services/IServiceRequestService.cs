using TechMove.GLMS.API.Models;
using TechMove.GLMS.API.Strategies;

namespace TechMove.GLMS.API.Services
{
    public class CreateServiceRequestDto
    {
        public int ContractId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal Amount { get; set; }
    }

    public interface IServiceRequestService
    {
        Task<List<ServiceRequest>> GetAllRequestsAsync();
        Task<List<ServiceRequest>> GetRequestsByContractAsync(int contractId);
        Task<ServiceRequest?> GetRequestByIdAsync(int id);
        Task<ServiceRequest> CreateServiceRequestAsync(CreateServiceRequestDto dto);
        Task<CurrencyConversionResult> CalculateCostAsync(decimal amount, string currency);
        Task<bool> UpdateRequestStatusAsync(int requestId, ServiceRequestStatus newStatus);
    }
}

