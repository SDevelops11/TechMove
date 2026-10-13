using Microsoft.Extensions.Logging;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Factories
{
    /// <summary>
    /// Factory implementation that encapsulates the instantiation of concrete contract classes.
    /// Eliminates tight coupling between client controllers and concrete contract types.
    /// </summary>
    public class ContractFactory : IContractFactory
    {
        private readonly IContractNumberGenerator _numberGenerator;
        private readonly ILogger<ContractFactory> _logger;

        public ContractFactory(IContractNumberGenerator numberGenerator, ILogger<ContractFactory> logger)
        {
            _numberGenerator = numberGenerator;
            _logger = logger;
        }

        public IContract CreateContract(string contractType, int clientId, string title, DateTime startDate, DateTime endDate)
        {
            IContract contract = contractType?.Trim().ToLowerInvariant() switch
            {
                "premium" => new PremiumContract(),
                "international" => new InternationalContract(),
                "express" => new ExpressContract(),
                "standard" or _ => new StandardContract()
            };

            contract.ContractNumber = _numberGenerator.GenerateContractNumber();
            contract.ClientId = clientId;
            contract.Title = title;
            contract.StartDate = startDate;
            contract.EndDate = endDate;
            contract.Status = ContractStatus.Draft;

            _logger.LogInformation("Factory generated {ContractType} with reference {ContractNumber}",
                contract.ContractType, contract.ContractNumber);

            return contract;
        }

        public Contract CreateEntity(string contractType, int clientId, string title, DateTime startDate, DateTime endDate, decimal baseValueUsd, decimal exchangeRate)
        {
            var domainContract = CreateContract(contractType, clientId, title, startDate, endDate);

            if (!domainContract.Validate(out var errors))
            {
                throw new InvalidOperationException($"Contract factory validation failed: {string.Join("; ", errors)}");
            }

            var calculatedUsd = domainContract.CalculateEstimatedCost(baseValueUsd);
            var calculatedZar = Math.Round(calculatedUsd * exchangeRate, 2);

            return new Contract
            {
                ContractNumber = domainContract.ContractNumber,
                ClientId = domainContract.ClientId,
                Title = domainContract.Title,
                StartDate = domainContract.StartDate,
                EndDate = domainContract.EndDate,
                Status = domainContract.Status,
                ServiceLevel = domainContract.ServiceLevel,
                ContractType = domainContract.ContractType,
                EstimatedValueUsd = calculatedUsd,
                EstimatedValueZar = calculatedZar,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}
