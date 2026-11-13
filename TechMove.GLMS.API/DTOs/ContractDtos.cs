using System.ComponentModel.DataAnnotations;
using TechMove.GLMS.API.Models;

namespace TechMove.GLMS.API.DTOs
{
    public class ContractDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ContractStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public ServiceLevel ServiceLevel { get; set; }
        public string ServiceLevelName => ServiceLevel.ToString();
        public string ContractType { get; set; } = "Standard";
        public decimal EstimatedValueUsd { get; set; }
        public decimal EstimatedValueZar { get; set; }
        public string? AgreementFileName { get; set; }
        public bool HasAgreementFile => !string.IsNullOrWhiteSpace(AgreementFileName);
        public DateTime CreatedAt { get; set; }
        public List<ContractStatusLogDto> StatusLogs { get; set; } = new();
        public int ActiveServiceRequestsCount { get; set; }
    }

    public class ContractStatusLogDto
    {
        public int Id { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class CreateContractRequestDto
    {
        [Required]
        public string ContractType { get; set; } = "Standard";

        [Required]
        public int ClientId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Range(100, 100_000_000, ErrorMessage = "Base value must be between $100 and $100,000,000.")]
        public decimal BaseValueUsd { get; set; }
    }

    public class UpdateContractStatusRequestDto
    {
        [Required]
        public ContractStatus NewStatus { get; set; }

        [Required(ErrorMessage = "A reason for status change must be supplied for audit tracking.")]
        [StringLength(500, MinimumLength = 3)]
        public string Reason { get; set; } = string.Empty;
    }

    public class ContractFilterQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ContractStatus? Status { get; set; }
        public int? ClientId { get; set; }
        public string? SearchTerm { get; set; }
    }
}
