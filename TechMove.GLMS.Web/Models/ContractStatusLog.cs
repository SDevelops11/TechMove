using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechMove.GLMS.Web.Models
{
    /// <summary>
    /// Audit log entry generated when a contract status transition occurs and Observer pattern notifies departments.
    /// </summary>
    public class ContractStatusLog
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }

        [ForeignKey(nameof(ContractId))]
        public virtual Contract? Contract { get; set; }

        [Required]
        [StringLength(50)]
        public string OldStatus { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string NewStatus { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string ActionTaken { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
