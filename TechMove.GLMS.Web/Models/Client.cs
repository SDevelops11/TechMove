using System.ComponentModel.DataAnnotations;

namespace TechMove.GLMS.Web.Models
{
    /// <summary>
    /// Represents an enterprise shipping and logistics client of TechMove.
    /// </summary>
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Client name is required")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Client name must be between 2 and 150 characters")]
        [Display(Name = "Client Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [StringLength(150)]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact phone is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(30)]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required")]
        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        [Display(Name = "Registered Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}
