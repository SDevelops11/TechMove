using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Services.ApiClients;
using TechMove.GLMS.Web.ViewModels;

namespace TechMove.GLMS.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IContractApiClient _contractApiClient;
        private readonly IServiceRequestApiClient _requestApiClient;
        private readonly ICurrencyApiClient _currencyApiClient;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IContractApiClient contractApiClient,
            IServiceRequestApiClient requestApiClient,
            ICurrencyApiClient currencyApiClient,
            ILogger<HomeController> logger)
        {
            _contractApiClient = contractApiClient;
            _requestApiClient = requestApiClient;
            _currencyApiClient = currencyApiClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var allContractsDto = await _contractApiClient.GetContractsAsync();
                var allRequestsDto = await _requestApiClient.GetAllRequestsAsync();
                var usdRate = await _currencyApiClient.GetExchangeRateAsync("USD", "ZAR");

                var allContracts = allContractsDto.Select(c => new Contract
                {
                    Id = c.Id,
                    ContractNumber = c.ContractNumber,
                    Title = c.Title,
                    ClientId = c.ClientId,
                    Client = new Client { Id = c.ClientId, Name = c.ClientName, Region = "Global" },
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Status = c.Status,
                    ServiceLevel = c.ServiceLevel,
                    ContractType = c.ContractType,
                    EstimatedValueUsd = c.EstimatedValueUsd,
                    EstimatedValueZar = c.EstimatedValueZar,
                    AgreementFileName = c.AgreementFileName,
                    CreatedAt = c.CreatedAt
                }).ToList();

                var allRequests = allRequestsDto.Select(r => new ServiceRequest
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    ContractId = r.ContractId,
                    Contract = new Contract { ContractNumber = r.ContractNumber, Title = r.ContractTitle },
                    Description = r.Description,
                    Origin = r.Origin,
                    Destination = r.Destination,
                    Cost = r.Cost,
                    OriginalCostUsd = r.OriginalCostUsd,
                    OriginalCurrency = r.OriginalCurrency,
                    ExchangeRateUsed = r.ExchangeRateUsed,
                    Status = r.Status,
                    CreatedDate = r.CreatedDate
                }).ToList();

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard metrics from Backend Web API");
                TempData["ErrorMessage"] = "Could not communicate with Backend Web API service. Ensure the API is running.";

                return View(new DashboardViewModel
                {
                    RecentContracts = new List<Contract>(),
                    RecentServiceRequests = new List<ServiceRequest>()
                });
            }
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
