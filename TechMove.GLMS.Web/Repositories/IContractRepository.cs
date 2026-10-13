using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Repositories
{
    public interface IContractRepository
    {
        Task<List<Contract>> GetAllAsync();
        Task<List<Contract>> FilterContractsAsync(DateTime? startDate, DateTime? endDate, ContractStatus? status, int? clientId, string? searchTerm);
        Task<Contract?> GetByIdAsync(int id, bool includeDetails = false);
        Task<Contract?> GetByContractNumberAsync(string contractNumber);
        Task AddAsync(Contract contract);
        Task UpdateAsync(Contract contract);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
