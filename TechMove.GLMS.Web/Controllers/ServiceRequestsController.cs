using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Services.ApiClients;
using TechMove.GLMS.Web.ViewModels;

namespace TechMove.GLMS.Web.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly IServiceRequestApiClient _requestApiClient;
        private readonly IContractApiClient _contractApiClient;
        private readonly ICurrencyApiClient _currencyApiClient;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(
            IServiceRequestApiClient requestApiClient,
            IContractApiClient contractApiClient,
            ICurrencyApiClient currencyApiClient,
            ILogger<ServiceRequestsController> logger)
        {
            _requestApiClient = requestApiClient;
            _contractApiClient = contractApiClient;
            _currencyApiClient = currencyApiClient;
            _logger = logger;
        }

        // GET: ServiceRequests
        public async Task<IActionResult> Index()
        {
            try
            {
                var dtos = await _requestApiClient.GetAllRequestsAsync();
                var requests = dtos.Select(MapToServiceRequest).ToList();
                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching service requests from Backend Web API");
                TempData["ErrorMessage"] = $"Backend Web API Error: {ex.Message}";
                return View(new List<ServiceRequest>());
            }
        }

        // GET: ServiceRequests/Create
        public async Task<IActionResult> Create(int? contractId)
        {
            try
            {
                var contracts = await _contractApiClient.GetContractsAsync();
                var rate = await _currencyApiClient.GetExchangeRateAsync("USD", "ZAR");

                var selectItems = contracts.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.ContractNumber} - {c.Title} ({c.Status})",
                    Selected = contractId.HasValue && c.Id == contractId.Value,
                    Disabled = c.Status == ContractStatus.Expired || c.Status == ContractStatus.OnHold
                }).ToList();

                var model = new CreateServiceRequestViewModel
                {
                    ContractId = contractId ?? (contracts.FirstOrDefault(c => c.Status == ContractStatus.Active)?.Id ?? 0),
                    Currency = "USD",
                    Amount = 1500,
                    CurrentExchangeRate = rate,
                    PreviewZarCost = Math.Round(1500 * rate, 2),
                    ContractsList = new SelectList(selectItems, "Value", "Text", contractId)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing service request creation form from Web API");
                TempData["ErrorMessage"] = $"Backend Web API Error: {ex.Message}";
                return View(new CreateServiceRequestViewModel());
            }
        }

        // POST: ServiceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateServiceRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateContractsDropdownAsync(model);
                return View(model);
            }

            try
            {
                var request = new ApiCreateServiceRequestRequest
                {
                    ContractId = model.ContractId,
                    Description = model.Description,
                    Origin = model.Origin,
                    Destination = model.Destination,
                    Currency = model.Currency,
                    Amount = model.Amount
                };

                var created = await _requestApiClient.CreateServiceRequestAsync(request);

                TempData["SuccessMessage"] =
                    $"Service Request {created.RequestNumber} created successfully via Backend Web API! " +
                    $"Amount: {created.OriginalCostUsd} {created.OriginalCurrency} -> R{created.Cost:N2} ZAR (Rate: {created.ExchangeRateUsed:F4})";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Backend Web API rejected service request creation: {Message}", ex.Message);
                ModelState.AddModelError(nameof(model.ContractId), ex.Message);
                await PopulateContractsDropdownAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service request via Backend Web API");
                ModelState.AddModelError(string.Empty, $"Communication Error: {ex.Message}");
                await PopulateContractsDropdownAsync(model);
                return View(model);
            }
        }

        // GET: ServiceRequests/ConvertCurrency (AJAX live API endpoint for dynamic client-side recalculation)
        [HttpGet]
        public async Task<IActionResult> ConvertCurrency(decimal amount, string currency = "USD")
        {
            try
            {
                var result = await _currencyApiClient.ConvertCurrencyAsync(amount, currency);
                return Json(new
                {
                    success = true,
                    amount = result.OriginalAmount,
                    currency = result.FromCurrency,
                    toCurrency = result.ToCurrency,
                    rate = result.ExchangeRate,
                    convertedZar = result.ConvertedAmount,
                    strategy = result.StrategyUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AJAX currency conversion error via Web API");
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        private async Task PopulateContractsDropdownAsync(CreateServiceRequestViewModel model)
        {
            try
            {
                var contracts = await _contractApiClient.GetContractsAsync();
                var selectItems = contracts.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.ContractNumber} - {c.Title} ({c.Status})",
                    Selected = c.Id == model.ContractId,
                    Disabled = c.Status == ContractStatus.Expired || c.Status == ContractStatus.OnHold
                }).ToList();

                model.ContractsList = new SelectList(selectItems, "Value", "Text", model.ContractId);
                model.CurrentExchangeRate = await _currencyApiClient.GetExchangeRateAsync(model.Currency, "ZAR");
            }
            catch
            {
                model.ContractsList = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        private static ServiceRequest MapToServiceRequest(ApiServiceRequestDto dto)
        {
            return new ServiceRequest
            {
                Id = dto.Id,
                RequestNumber = dto.RequestNumber,
                ContractId = dto.ContractId,
                Contract = new Contract
                {
                    Id = dto.ContractId,
                    ContractNumber = dto.ContractNumber,
                    Title = dto.ContractTitle,
                    Client = new Client { Name = dto.ClientName, Region = "Global" }
                },
                Description = dto.Description,
                Origin = dto.Origin,
                Destination = dto.Destination,
                Cost = dto.Cost,
                OriginalCostUsd = dto.OriginalCostUsd,
                OriginalCurrency = dto.OriginalCurrency,
                ExchangeRateUsed = dto.ExchangeRateUsed,
                Status = dto.Status,
                CreatedDate = dto.CreatedDate
            };
        }
    }
}
