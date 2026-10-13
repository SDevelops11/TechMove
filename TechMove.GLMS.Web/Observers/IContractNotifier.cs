using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Observers
{
    public interface IContractNotifier
    {
        void Attach(IContractObserver observer);
        void Detach(IContractObserver observer);
        Task<List<ContractStatusLog>> NotifyAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null);
    }
}
