using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Services;
using TechMove.GLMS.Web.ViewModels;

namespace TechMove.GLMS.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IContractService _contractService;
        private readonly IServiceRequestService _requestService;
        private readonly IExchangeRateService _rateService;

        public HomeController(
            IContractService contractService,
            IServiceRequestService requestService,
            IExchangeRateService rateService)
        {
            _contractService = contractService;
            _requestService = requestService;
            _rateService = rateService;
        }

        public async Task<IActionResult> Index()
        {
            var allContracts = await _contractService.GetAllContractsAsync();
            var allRequests = await _requestService.GetAllRequestsAsync();
            var usdRate = await _rateService.GetExchangeRateAsync("USD", "ZAR");

            var model = new DashboardViewModel
            {
                TotalContracts = allContracts.Count,
                ActiveContracts = allContracts.Count(c => c.Status == ContractStatus.Active),
                OnHoldContracts = allContracts.Count(c => c.Status == ContractStatus.OnHold),
                ExpiredContracts = allContracts.Count(c => c.Status == ContractStatus.Expired),
                DraftContracts = allContracts.Count(c => c.Status == ContractStatus.Draft),
                TotalServiceRequests = allRequests.Count,
                TotalContractValueZar = allContracts.Sum(c => c.EstimatedValueZar),
                TotalDispatchedCostZar = allRequests.Sum(r => r.Cost),
                CurrentUsdZarRate = usdRate,
                RecentContracts = allContracts.Take(5).ToList(),
                RecentServiceRequests = allRequests.Take(5).ToList()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
