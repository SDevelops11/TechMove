using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Repositories;
using TechMove.GLMS.Web.Services;
using TechMove.GLMS.Web.ViewModels;

namespace TechMove.GLMS.Web.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly IServiceRequestService _requestService;
        private readonly IContractRepository _contractRepository;
        private readonly IExchangeRateService _rateService;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(
            IServiceRequestService requestService,
            IContractRepository contractRepository,
            IExchangeRateService rateService,
            ILogger<ServiceRequestsController> logger)
        {
            _requestService = requestService;
            _contractRepository = contractRepository;
            _rateService = rateService;
            _logger = logger;
        }

        // GET: ServiceRequests
        public async Task<IActionResult> Index()
        {
            var requests = await _requestService.GetAllRequestsAsync();
            return View(requests);
        }

        // GET: ServiceRequests/Create
        public async Task<IActionResult> Create(int? contractId)
        {
            var contracts = await _contractRepository.GetAllAsync();
            var rate = await _rateService.GetExchangeRateAsync("USD", "ZAR");

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

        // POST: ServiceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateServiceRequestViewModel model)
        {
            // First check contract validity
            var contract = await _contractRepository.GetByIdAsync(model.ContractId);
            if (contract == null)
            {
                ModelState.AddModelError(nameof(model.ContractId), "Selected contract does not exist.");
            }
            else if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
            {
                // CRITICAL POE WORKFLOW RULE
                ModelState.AddModelError(
                    nameof(model.ContractId),
                    $"Workflow Policy Violation: Contract '{contract.ContractNumber}' is currently in '{contract.Status}' status. Service Requests cannot be raised against Expired or On Hold contracts.");
            }

            if (!ModelState.IsValid)
            {
                var contracts = await _contractRepository.GetAllAsync();
                var selectItems = contracts.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.ContractNumber} - {c.Title} ({c.Status})",
                    Selected = c.Id == model.ContractId,
                    Disabled = c.Status == ContractStatus.Expired || c.Status == ContractStatus.OnHold
                }).ToList();

                model.ContractsList = new SelectList(selectItems, "Value", "Text", model.ContractId);
                model.CurrentExchangeRate = await _rateService.GetExchangeRateAsync(model.Currency, "ZAR");
                return View(model);
            }

            try
            {
                var dto = new CreateServiceRequestDto
                {
                    ContractId = model.ContractId,
                    Description = model.Description,
                    Origin = model.Origin,
                    Destination = model.Destination,
                    Currency = model.Currency,
                    Amount = model.Amount
                };

                var createdRequest = await _requestService.CreateServiceRequestAsync(dto);

                TempData["SuccessMessage"] =
                    $"Service Request {createdRequest.RequestNumber} created successfully! Amount: {createdRequest.OriginalCostUsd} {createdRequest.OriginalCurrency} -> R{createdRequest.Cost:N2} ZAR (Rate: {createdRequest.ExchangeRateUsed:F4})";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service request");
                ModelState.AddModelError(string.Empty, ex.Message);

                var contracts = await _contractRepository.GetAllAsync();
                var selectItems = contracts.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.ContractNumber} - {c.Title} ({c.Status})",
                    Selected = c.Id == model.ContractId,
                    Disabled = c.Status == ContractStatus.Expired || c.Status == ContractStatus.OnHold
                }).ToList();

                model.ContractsList = new SelectList(selectItems, "Value", "Text", model.ContractId);
                model.CurrentExchangeRate = await _rateService.GetExchangeRateAsync(model.Currency, "ZAR");
                return View(model);
            }
        }

        // GET: ServiceRequests/ConvertCurrency (AJAX live API endpoint for dynamic client-side recalculation)
        [HttpGet]
        public async Task<IActionResult> ConvertCurrency(decimal amount, string currency = "USD")
        {
            try
            {
                var result = await _requestService.CalculateCostAsync(amount, currency);
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
                _logger.LogError(ex, "AJAX currency conversion error");
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }
    }
}
