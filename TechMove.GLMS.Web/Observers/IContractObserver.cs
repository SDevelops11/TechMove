using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Observers
{
    /// <summary>
    /// Observer interface for inter-departmental notifications when a contract status changes.
    /// Matches Part 1 Enterprise Architecture specification.
    /// </summary>
    public interface IContractObserver
    {
        string DepartmentName { get; }
        Task<ContractStatusLog> OnContractStatusChangedAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null);
    }
}
