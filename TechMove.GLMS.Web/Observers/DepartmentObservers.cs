using Microsoft.Extensions.Logging;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Observers
{
    /// <summary>
    /// Operations Department Observer: Schedules or releases transport corridors, driver rosters, and container depots.
    /// </summary>
    public class OperationsDepartmentObserver : IContractObserver
    {
        private readonly ILogger<OperationsDepartmentObserver> _logger;
        public string DepartmentName => "Operations Department";

        public OperationsDepartmentObserver(ILogger<OperationsDepartmentObserver> logger)
        {
            _logger = logger;
        }

        public Task<ContractStatusLog> OnContractStatusChangedAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null)
        {
            string action = newStatus switch
            {
                ContractStatus.Active => $"Provisioned freight transit assets and designated driver scheduling corridors for {contract.ContractNumber}.",
                ContractStatus.OnHold => $"Temporarily suspended active transit routes and parked freight handling orders for {contract.ContractNumber}.",
                ContractStatus.Expired => $"Decommissioned fleet allocations and released regional container reservations for {contract.ContractNumber}.",
                ContractStatus.Draft => $"Initialized resource requirement projections for upcoming agreement {contract.ContractNumber}.",
                _ => $"Updated operational logging for status: {newStatus}."
            };

            _logger.LogInformation("[{Dept}] {Action}", DepartmentName, action);

            return Task.FromResult(new ContractStatusLog
            {
                ContractId = contract.Id,
                OldStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                Department = DepartmentName,
                ActionTaken = action,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Finance Department Observer: Configures billing cycles, credit facilities, invoice holds, or account closeout.
    /// </summary>
    public class FinanceDepartmentObserver : IContractObserver
    {
        private readonly ILogger<FinanceDepartmentObserver> _logger;
        public string DepartmentName => "Finance Department";

        public FinanceDepartmentObserver(ILogger<FinanceDepartmentObserver> logger)
        {
            _logger = logger;
        }

        public Task<ContractStatusLog> OnContractStatusChangedAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null)
        {
            string action = newStatus switch
            {
                ContractStatus.Active => $"Activated client credit ledger and established recurring SLA billing schedules (Estimated ZAR: R{contract.EstimatedValueZar:N2}).",
                ContractStatus.OnHold => $"Placed billing freezes and placed a hold on pending automated invoices for {contract.ContractNumber}.",
                ContractStatus.Expired => $"Issued final reconciliation statement and closed accounts receivable lifecycle for {contract.ContractNumber}.",
                ContractStatus.Draft => $"Drafted preliminary tariff quotation and exchange rate risk margin for {contract.ContractNumber}.",
                _ => $"Updated finance ledger tracking."
            };

            _logger.LogInformation("[{Dept}] {Action}", DepartmentName, action);

            return Task.FromResult(new ContractStatusLog
            {
                ContractId = contract.Id,
                OldStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                Department = DepartmentName,
                ActionTaken = action,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Client Services Observer: Communicates status briefings, sends formal notification emails, and maintains SLA tickets.
    /// </summary>
    public class ClientServicesObserver : IContractObserver
    {
        private readonly ILogger<ClientServicesObserver> _logger;
        public string DepartmentName => "Client Services";

        public ClientServicesObserver(ILogger<ClientServicesObserver> logger)
        {
            _logger = logger;
        }

        public Task<ContractStatusLog> OnContractStatusChangedAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null)
        {
            string action = newStatus switch
            {
                ContractStatus.Active => $"Dispatched official SLA activation certificate and client portal access instructions.",
                ContractStatus.OnHold => $"Notified client account manager regarding operational pause: {(string.IsNullOrEmpty(note) ? "Administrative Review" : note)}.",
                ContractStatus.Expired => $"Notified client representative regarding agreement conclusion and presented renewal contract proposal options.",
                ContractStatus.Draft => $"Sent draft agreement copy for client legal review.",
                _ => $"Logged client services notification."
            };

            _logger.LogInformation("[{Dept}] {Action}", DepartmentName, action);

            return Task.FromResult(new ContractStatusLog
            {
                ContractId = contract.Id,
                OldStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                Department = DepartmentName,
                ActionTaken = action,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Compliance Department Observer: Checks customs declarations, border clearance bonds, and regulatory conformity.
    /// </summary>
    public class ComplianceDepartmentObserver : IContractObserver
    {
        private readonly ILogger<ComplianceDepartmentObserver> _logger;
        public string DepartmentName => "Compliance & Legal";

        public ComplianceDepartmentObserver(ILogger<ComplianceDepartmentObserver> logger)
        {
            _logger = logger;
        }

        public Task<ContractStatusLog> OnContractStatusChangedAsync(Contract contract, ContractStatus oldStatus, ContractStatus newStatus, string? note = null)
        {
            string action = newStatus switch
            {
                ContractStatus.Active => $"Verified international shipping compliance, cross-border bonds, and verified PDF signed contract.",
                ContractStatus.OnHold => $"Initiated regulatory compliance audit on {contract.ContractNumber}.",
                ContractStatus.Expired => $"Archived compliance documentation in 5-year statutory retention register.",
                ContractStatus.Draft => $"Vetted preliminary terms against international maritime transport regulations.",
                _ => $"Logged compliance status audit."
            };

            _logger.LogInformation("[{Dept}] {Action}", DepartmentName, action);

            return Task.FromResult(new ContractStatusLog
            {
                ContractId = contract.Id,
                OldStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                Department = DepartmentName,
                ActionTaken = action,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
