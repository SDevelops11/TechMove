using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechMove.GLMS.Web.Models;

namespace TechMove.GLMS.Web.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalContracts { get; set; }
        public int ActiveContracts { get; set; }
        public int OnHoldContracts { get; set; }
        public int ExpiredContracts { get; set; }
        public int DraftContracts { get; set; }
        public int TotalServiceRequests { get; set; }
        public decimal TotalContractValueZar { get; set; }
        public decimal TotalDispatchedCostZar { get; set; }
        public decimal CurrentUsdZarRate { get; set; }
        public List<Contract> RecentContracts { get; set; } = new();
        public List<ServiceRequest> RecentServiceRequests { get; set; } = new();
    }

    public class ContractIndexViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Status Filter")]
        public ContractStatus? Status { get; set; }

        [Display(Name = "Client Filter")]
        public int? ClientId { get; set; }

        [Display(Name = "Keyword Search")]
        public string? SearchTerm { get; set; }

        public List<Contract> Contracts { get; set; } = new();
        public SelectList? ClientsList { get; set; }
    }

    public class CreateContractViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        [Display(Name = "Contract Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client is required")]
        [Display(Name = "Select Client")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Contract Type is required")]
        [Display(Name = "Contract Type (Factory Pattern)")]
        public string ContractType { get; set; } = "Standard";

        [Required(ErrorMessage = "Start Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "End Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(6);

        [Required(ErrorMessage = "Base estimated value is required")]
        [Range(1, 1000000000, ErrorMessage = "Estimated value must be greater than zero")]
        [Display(Name = "Base Estimated Value (USD)")]
        public decimal BaseValueUsd { get; set; } = 50000;

        [Display(Name = "Signed Agreement (PDF only)")]
        public IFormFile? AgreementPdf { get; set; }

        public SelectList? ClientsList { get; set; }
    }

    public class CreateServiceRequestViewModel
    {
        [Required(ErrorMessage = "Contract is required")]
        [Display(Name = "Select Contract")]
        public int ContractId { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Description must be between 5 and 500 characters")]
        [Display(Name = "Service / Freight Description")]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Origin Hub / Port")]
        public string Origin { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Destination Hub / Port")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "Currency is required")]
        [Display(Name = "Billing Currency")]
        public string Currency { get; set; } = "USD";

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, 100000000, ErrorMessage = "Amount must be strictly greater than zero")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; } = 1000;

        [Display(Name = "Estimated Converted Cost (ZAR)")]
        public decimal PreviewZarCost { get; set; }

        public decimal CurrentExchangeRate { get; set; }

        public SelectList? ContractsList { get; set; }
    }

    public class ChangeContractStatusViewModel
    {
        public int ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractTitle { get; set; } = string.Empty;
        public ContractStatus CurrentStatus { get; set; }

        [Required(ErrorMessage = "New status is required")]
        [Display(Name = "New Status")]
        public ContractStatus NewStatus { get; set; }

        [StringLength(300)]
        [Display(Name = "Reason / Operational Notes")]
        public string? Reason { get; set; }
    }
}
