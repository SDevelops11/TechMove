using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.Factories
{
    public abstract class BaseContract : IContract
    {
        public abstract string ContractType { get; }
        public string ContractNumber { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ContractStatus Status { get; set; } = ContractStatus.Draft;
        public abstract ServiceLevel ServiceLevel { get; }
        public abstract decimal BaseRateMultiplier { get; }

        public virtual bool Validate(out List<string> validationErrors)
        {
            validationErrors = new List<string>();

            if (ClientId <= 0)
                validationErrors.Add("A valid Client must be associated with the contract.");

            if (string.IsNullOrWhiteSpace(Title))
                validationErrors.Add("Contract title is required.");

            if (EndDate <= StartDate)
                validationErrors.Add("Contract end date must be after the start date.");

            return validationErrors.Count == 0;
        }

        public decimal CalculateEstimatedCost(decimal baseValueUsd)
        {
            if (baseValueUsd < 0) return 0;
            return Math.Round(baseValueUsd * BaseRateMultiplier, 2);
        }

        public virtual Contract ToEntity(decimal exchangeRate)
        {
            var calculatedUsd = CalculateEstimatedCost(10000m); // default base
            return new Contract
            {
                ContractNumber = ContractNumber,
                ClientId = ClientId,
                Title = Title,
                StartDate = StartDate,
                EndDate = EndDate,
                Status = Status,
                ServiceLevel = ServiceLevel,
                ContractType = ContractType,
                EstimatedValueUsd = calculatedUsd,
                EstimatedValueZar = Math.Round(calculatedUsd * exchangeRate, 2),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Standard freight contract: Baseline terms, standard SLA turnaround.
    /// </summary>
    public class StandardContract : BaseContract
    {
        public override string ContractType => "Standard";
        public override ServiceLevel ServiceLevel => ServiceLevel.Standard;
        public override decimal BaseRateMultiplier => 1.00m;
    }

    /// <summary>
    /// Premium freight contract: Priority haulage corridors, 24/7 dedicated fleet, multiplier 1.25.
    /// </summary>
    public class PremiumContract : BaseContract
    {
        public override string ContractType => "Premium";
        public override ServiceLevel ServiceLevel => ServiceLevel.Premium;
        public override decimal BaseRateMultiplier => 1.25m;

        public override bool Validate(out List<string> validationErrors)
        {
            base.Validate(out validationErrors);
            var durationDays = (EndDate - StartDate).TotalDays;
            if (durationDays < 30)
            {
                validationErrors.Add("Premium SLA contracts must have a minimum duration of 30 days.");
            }
            return validationErrors.Count == 0;
        }
    }

    /// <summary>
    /// International freight contract: Cross-border documentation, customs clearance, multiplier 1.50.
    /// </summary>
    public class InternationalContract : BaseContract
    {
        public override string ContractType => "International";
        public override ServiceLevel ServiceLevel => ServiceLevel.International;
        public override decimal BaseRateMultiplier => 1.50m;

        public override bool Validate(out List<string> validationErrors)
        {
            base.Validate(out validationErrors);
            var durationDays = (EndDate - StartDate).TotalDays;
            if (durationDays < 60)
            {
                validationErrors.Add("International logistics agreements require at least a 60-day operational scope.");
            }
            return validationErrors.Count == 0;
        }
    }

    /// <summary>
    /// Express freight contract: High-priority, urgent air/road dispatch, multiplier 1.75.
    /// </summary>
    public class ExpressContract : BaseContract
    {
        public override string ContractType => "Express";
        public override ServiceLevel ServiceLevel => ServiceLevel.Express;
        public override decimal BaseRateMultiplier => 1.75m;
    }
}
