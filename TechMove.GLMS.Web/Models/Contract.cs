using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechMove.GLMS.Web.Models
{
    /// <summary>
    /// Represents a legal logistics agreement between TechMove and a Client.
    /// </summary>
    public class Contract
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Contract Number")]
        public string ContractNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        [Display(Name = "Contract Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client is required")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual Client? Client { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [Required]
        [Display(Name = "Service Level")]
        public ServiceLevel ServiceLevel { get; set; } = ServiceLevel.Standard;

        [Required]
        [StringLength(50)]
        [Display(Name = "Contract Type")]
        public string ContractType { get; set; } = "Standard";

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1000000000, ErrorMessage = "Value must be positive")]
        [Display(Name = "Estimated Value (USD)")]
        public decimal EstimatedValueUsd { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Estimated Value (ZAR)")]
        public decimal EstimatedValueZar { get; set; }

        [StringLength(255)]
        [Display(Name = "Signed Agreement File")]
        public string? AgreementFileName { get; set; }

        [StringLength(500)]
        [Display(Name = "Signed Agreement Path")]
        public string? AgreementFilePath { get; set; }

        [Display(Name = "Uploaded At")]
        public DateTime? AgreementUploadedAt { get; set; }

        public long? AgreementFileSizeBytes { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Updated")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
        public virtual ICollection<ContractStatusLog> StatusLogs { get; set; } = new List<ContractStatusLog>();

        [NotMapped]
        public bool IsActive => Status == ContractStatus.Active;

        [NotMapped]
        public bool AllowsNewRequests => Status == ContractStatus.Active;
    }
}
