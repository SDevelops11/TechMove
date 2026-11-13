using TechMove.GLMS.API.Models;

namespace TechMove.GLMS.API.Observers
{
    public interface IContractNotifier
    {
        void Attach(IContractObserver observer);
        void Detach(IContractObserver observer);
        Task<List<ContractStatusLog>> NotifyAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null);
    }
}

