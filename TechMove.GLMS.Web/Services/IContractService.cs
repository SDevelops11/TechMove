using Microsoft.AspNetCore.Http;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Services
{
    public interface IContractService
    {
        Task<List<Contract>> GetAllContractsAsync();
        Task<List<Contract>> SearchAndFilterContractsAsync(DateTime? startDate, DateTime? endDate, ContractStatus? status, int? clientId, string? searchTerm);
        Task<Contract?> GetContractDetailsAsync(int id);
        Task<Contract> CreateContractAsync(string contractType, int clientId, string title, DateTime startDate, DateTime endDate, decimal baseValueUsd, IFormFile? agreementPdf);
        Task<bool> UpdateContractStatusAsync(int contractId, ContractStatus newStatus, string? reason = null);
        Task<bool> UploadSignedAgreementAsync(int contractId, IFormFile agreementPdf);
        bool CanTransitionStatus(ContractStatus currentStatus, ContractStatus newStatus, out string? errorMessage);
    }
}
