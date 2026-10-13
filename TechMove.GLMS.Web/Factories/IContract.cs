using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Factories
{
    /// <summary>
    /// Common interface for all contract types created via the Factory pattern.
    /// Matches the design specified in Part 1 Enterprise Architecture Report.
    /// </summary>
    public interface IContract
    {
        string ContractType { get; }
        string ContractNumber { get; set; }
        int ClientId { get; set; }
        string Title { get; set; }
        DateTime StartDate { get; set; }
        DateTime EndDate { get; set; }
        ContractStatus Status { get; set; }
        ServiceLevel ServiceLevel { get; }
        decimal BaseRateMultiplier { get; }

        /// <summary>
        /// Validates contract business rules specific to the contract type.
        /// </summary>
        bool Validate(out List<string> validationErrors);

        /// <summary>
        /// Calculates estimated contract cost based on base value and service tier multiplier.
        /// </summary>
        decimal CalculateEstimatedCost(decimal baseValueUsd);

        /// <summary>
        /// Converts the factory domain contract into an EF Core Contract entity.
        /// </summary>
        Contract ToEntity(decimal exchangeRate);
    }
}
