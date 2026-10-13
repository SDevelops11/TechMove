using Microsoft.Extensions.Logging;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Observers
{
    /// <summary>
    /// Subject implementation for the Observer pattern.
    /// Manages the collection of observers and broadcasts contract status lifecycle events.
    /// </summary>
    public class ContractNotifier : IContractNotifier
    {
        private readonly List<IContractObserver> _observers = new();
        private readonly ILogger<ContractNotifier> _logger;

        public ContractNotifier(IEnumerable<IContractObserver> observers, ILogger<ContractNotifier> logger)
        {
            _observers.AddRange(observers);
            _logger = logger;
        }

        public void Attach(IContractObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
                _logger.LogInformation("Attached observer: {Observer}", observer.DepartmentName);
            }
        }

        public void Detach(IContractObserver observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
                _logger.LogInformation("Detached observer: {Observer}", observer.DepartmentName);
            }
        }

        public async Task<List<ContractStatusLog>> NotifyAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null)
        {
            _logger.LogInformation("Broadcasting status change for contract {ContractNumber}: {OldStatus} -> {NewStatus}",
                contract.ContractNumber, oldStatus, newStatus);

            var logs = new List<ContractStatusLog>();

            foreach (var observer in _observers)
            {
                try
                {
                    var logEntry = await observer.OnContractStatusChangedAsync(contract, oldStatus, newStatus, note);
                    logs.Add(logEntry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying observer {Observer} for contract {ContractNumber}",
                        observer.DepartmentName, contract.ContractNumber);
                }
            }

            return logs;
        }
    }
}
