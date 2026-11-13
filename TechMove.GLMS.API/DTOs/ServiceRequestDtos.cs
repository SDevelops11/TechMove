using System.ComponentModel.DataAnnotations;
using TechMove.GLMS.API.Models;

namespace TechMove.GLMS.API.DTOs
{
    public class ServiceRequestDto
    {
        public int Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractTitle { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public decimal OriginalCostUsd { get; set; }
        public string OriginalCurrency { get; set; } = "USD";
        public decimal ExchangeRateUsed { get; set; }
        public ServiceRequestStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime CreatedDate { get; set; }
    }

    public class CreateServiceRequestRequestDto
    {
        [Required]
        public int ContractId { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 5)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Origin { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(USD|EUR|GBP)$", ErrorMessage = "Currency must be USD, EUR, or GBP.")]
        public string Currency { get; set; } = "USD";

        [Required]
        [Range(1, 10_000_000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
