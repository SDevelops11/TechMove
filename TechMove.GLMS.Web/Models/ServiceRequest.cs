using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechMove.GLMS.Web.Models
{
    /// <summary>
    /// Represents an operational logistics dispatch/service request raised under an active contract.
    /// </summary>
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Request Number")]
        public string RequestNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contract is required")]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        [ForeignKey(nameof(ContractId))]
        public virtual Contract? Contract { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 500 characters")]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Origin Port / Hub")]
        public string Origin { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Destination Port / Hub")]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Display(Name = "Currency")]
        public string OriginalCurrency { get; set; } = "USD";

        [Required(ErrorMessage = "Amount in foreign currency is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than zero")]
        [Display(Name = "Original Amount")]
        public decimal OriginalCostUsd { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        [Display(Name = "Exchange Rate")]
        public decimal ExchangeRateUsed { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000000, ErrorMessage = "Converted ZAR cost must be greater than zero")]
        [Display(Name = "Total Cost (ZAR)")]
        public decimal Cost { get; set; }

        [Required]
        [Display(Name = "Request Status")]
        public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
