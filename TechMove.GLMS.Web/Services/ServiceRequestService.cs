using Microsoft.Extensions.Logging;
using TechMove.GLMS.Web.Factories;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Repositories;
using TechMove.GLMS.Web.Strategies;

namespace TechMove.GLMS.Web.Services
{
    public class ServiceRequestService : IServiceRequestService
    {
        private readonly IServiceRequestRepository _requestRepository;
        private readonly IContractRepository _contractRepository;
        private readonly ICurrencyConversionContext _currencyContext;
        private readonly IContractNumberGenerator _numberGenerator;
        private readonly ILogger<ServiceRequestService> _logger;

        public ServiceRequestService(
            IServiceRequestRepository requestRepository,
            IContractRepository contractRepository,
            ICurrencyConversionContext currencyContext,
            IContractNumberGenerator numberGenerator,
            ILogger<ServiceRequestService> logger)
        {
            _requestRepository = requestRepository;
            _contractRepository = contractRepository;
            _currencyContext = currencyContext;
            _numberGenerator = numberGenerator;
            _logger = logger;
        }

        public async Task<List<ServiceRequest>> GetAllRequestsAsync()
        {
            return await _requestRepository.GetAllAsync();
        }

        public async Task<List<ServiceRequest>> GetRequestsByContractAsync(int contractId)
        {
            return await _requestRepository.GetByContractIdAsync(contractId);
        }

        public async Task<ServiceRequest?> GetRequestByIdAsync(int id)
        {
            return await _requestRepository.GetByIdAsync(id);
        }

        public async Task<CurrencyConversionResult> CalculateCostAsync(decimal amount, string currency)
        {
            currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
            return await _currencyContext.ExecuteConversionAsync(amount, currency, "ZAR");
        }

        public async Task<ServiceRequest> CreateServiceRequestAsync(CreateServiceRequestDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Description))
            {
                throw new ArgumentException("Service request description is required.", nameof(dto.Description));
            }

            if (dto.Amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.", nameof(dto.Amount));
            }

            // 1. Fetch parent contract
            var contract = await _contractRepository.GetByIdAsync(dto.ContractId);
            if (contract == null)
            {
                throw new KeyNotFoundException($"Parent contract with ID {dto.ContractId} was not found.");
            }

            // 2. CRITICAL POE WORKFLOW RULE:
            // "A ServiceRequest cannot be created if the parent Contract is set to 'Expired' or 'On Hold'."
            if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
            {
                _logger.LogWarning("Attempted to raise service request against {Status} contract {ContractNumber}",
                    contract.Status, contract.ContractNumber);

                throw new InvalidOperationException(
                    $"Workflow Validation Error: A Service Request cannot be created because the parent Contract '{contract.ContractNumber}' is currently in '{contract.Status}' status. Requests may only be raised against Active contracts.");
            }

            if (contract.Status != ContractStatus.Active)
            {
                throw new InvalidOperationException(
                    $"Workflow Validation Error: Contract '{contract.ContractNumber}' is in '{contract.Status}' status. A contract must be Active before service requests can be dispatched.");
            }

            // 3. Utilize Strategy pattern with live external currency conversion to calculate ZAR cost
            string currency = string.IsNullOrWhiteSpace(dto.Currency) ? "USD" : dto.Currency.Trim().ToUpperInvariant();
            var conversion = await _currencyContext.ExecuteConversionAsync(dto.Amount, currency, "ZAR");

            // 4. Construct service request entity
            var request = new ServiceRequest
            {
                RequestNumber = _numberGenerator.GenerateServiceRequestNumber(),
                ContractId = dto.ContractId,
                Description = dto.Description.Trim(),
                Origin = dto.Origin?.Trim() ?? string.Empty,
                Destination = dto.Destination?.Trim() ?? string.Empty,
                OriginalCurrency = currency,
                OriginalCostUsd = dto.Amount,
                ExchangeRateUsed = conversion.ExchangeRate,
                Cost = conversion.ConvertedAmount,
                Status = ServiceRequestStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            await _requestRepository.AddAsync(request);

            _logger.LogInformation("Successfully raised Service Request {RequestNumber} under contract {ContractNumber}. Cost: R{Cost:N2} ZAR",
                request.RequestNumber, contract.ContractNumber, request.Cost);

            return request;
        }

        public async Task<bool> UpdateRequestStatusAsync(int requestId, ServiceRequestStatus newStatus)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);
            if (request == null) return false;

            request.Status = newStatus;
            await _requestRepository.UpdateAsync(request);
            return true;
        }
    }
}
