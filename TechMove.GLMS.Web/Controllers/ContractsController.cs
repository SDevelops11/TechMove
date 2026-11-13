using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Services.ApiClients;
using TechMove.GLMS.Web.ViewModels;

namespace TechMove.GLMS.Web.Controllers
{
    public class ContractsController : Controller
    {
        private readonly IContractApiClient _contractApiClient;
        private readonly IClientApiClient _clientApiClient;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(
            IContractApiClient contractApiClient,
            IClientApiClient clientApiClient,
            ILogger<ContractsController> logger)
        {
            _contractApiClient = contractApiClient;
            _clientApiClient = clientApiClient;
            _logger = logger;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            ContractStatus? status,
            int? clientId,
            string? searchTerm)
        {
            try
            {
                var dtos = await _contractApiClient.GetContractsAsync(startDate, endDate, status, clientId, searchTerm);
                var contracts = dtos.Select(MapToContract).ToList();

                var clients = await _clientApiClient.GetClientsAsync();

                var model = new ContractIndexViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = status,
                    ClientId = clientId,
                    SearchTerm = searchTerm,
                    Contracts = contracts,
                    ClientsList = new SelectList(clients, "Id", "Name", clientId)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load contracts from Backend Web API");
                TempData["ErrorMessage"] = $"Backend Web API Communication Error: {ex.Message}";
                return View(new ContractIndexViewModel());
            }
        }

        // GET: Contracts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var dto = await _contractApiClient.GetContractByIdAsync(id);
                if (dto == null)
                {
                    TempData["ErrorMessage"] = $"Contract #{id} was not found on the backend API service.";
                    return RedirectToAction(nameof(Index));
                }

                var contract = MapToContract(dto);
                return View(contract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contract details from Web API for ID {Id}", id);
                TempData["ErrorMessage"] = $"Web API Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Contracts/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var clients = await _clientApiClient.GetClientsAsync();
                var model = new CreateContractViewModel
                {
                    ClientsList = new SelectList(clients, "Id", "Name")
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client list from Web API");
                TempData["ErrorMessage"] = "Could not communicate with Backend Web API to load client records.";
                return View(new CreateContractViewModel());
            }
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContractViewModel model)
        {
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "Contract end date must be after start date.");
            }

            if (model.AgreementPdf != null && model.AgreementPdf.Length > 0)
            {
                var ext = Path.GetExtension(model.AgreementPdf.FileName).ToLowerInvariant();
                if (ext != ".pdf" || model.AgreementPdf.ContentType != "application/pdf")
                {
                    ModelState.AddModelError(nameof(model.AgreementPdf), "Only valid PDF agreements (.pdf) are permitted.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateClientsDropdownAsync(model);
                return View(model);
            }

            try
            {
                var request = new ApiCreateContractRequest
                {
                    ContractType = model.ContractType,
                    ClientId = model.ClientId,
                    Title = model.Title,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    BaseValueUsd = model.BaseValueUsd
                };

                var createdDto = await _contractApiClient.CreateContractAsync(request, model.AgreementPdf);
                TempData["SuccessMessage"] = $"Contract {createdDto.ContractNumber} created successfully via Backend Web API (GoF Factory Pattern)!";
                return RedirectToAction(nameof(Details), new { id = createdDto.Id });
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "Web API rejected contract creation: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, $"Web API Error ({(int)ex.StatusCode}): {ex.Message}");
                await PopulateClientsDropdownAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error communicating with Web API during contract creation");
                ModelState.AddModelError(string.Empty, $"Communication Error: {ex.Message}");
                await PopulateClientsDropdownAsync(model);
                return View(model);
            }
        }

        // GET: Contracts/ChangeStatus/5
        public async Task<IActionResult> ChangeStatus(int id)
        {
            try
            {
                var dto = await _contractApiClient.GetContractByIdAsync(id);
                if (dto == null)
                {
                    TempData["ErrorMessage"] = "Contract not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new ChangeContractStatusViewModel
                {
                    ContractId = dto.Id,
                    ContractNumber = dto.ContractNumber,
                    ContractTitle = dto.Title,
                    CurrentStatus = dto.Status,
                    NewStatus = dto.Status
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching contract for status update from Web API");
                TempData["ErrorMessage"] = $"Web API Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Contracts/ChangeStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(ChangeContractStatusViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var updated = await _contractApiClient.UpdateStatusAsync(model.ContractId, model.NewStatus, model.Reason ?? "Status updated via web client");
                TempData["SuccessMessage"] = $"Contract status successfully updated to '{updated.Status}' via Backend Web API (GoF Observer Pattern triggered)!";
                return RedirectToAction(nameof(Details), new { id = model.ContractId });
            }
            catch (ApiException ex)
            {
                _logger.LogWarning(ex, "API rejected status update: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, $"Web API Policy Rejection: {ex.Message}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status via Web API");
                ModelState.AddModelError(string.Empty, $"Communication failure: {ex.Message}");
                return View(model);
            }
        }

        // GET: Contracts/DownloadAgreement/5
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            try
            {
                var fileResult = await _contractApiClient.DownloadAgreementAsync(id);
                if (fileResult == null)
                {
                    TempData["ErrorMessage"] = "No signed agreement found for this contract on the server storage.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return File(fileResult.Value.FileBytes, fileResult.Value.ContentType, fileResult.Value.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download agreement via Web API for ID {Id}", id);
                TempData["ErrorMessage"] = $"Could not download agreement: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        private async Task PopulateClientsDropdownAsync(CreateContractViewModel model)
        {
            try
            {
                var clients = await _clientApiClient.GetClientsAsync();
                model.ClientsList = new SelectList(clients, "Id", "Name", model.ClientId);
            }
            catch
            {
                model.ClientsList = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        private static Contract MapToContract(ApiContractDto dto)
        {
            return new Contract
            {
                Id = dto.Id,
                ContractNumber = dto.ContractNumber,
                Title = dto.Title,
                ClientId = dto.ClientId,
                Client = new Client
                {
                    Id = dto.ClientId,
                    Name = dto.ClientName,
                    Region = "Global"
                },
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status,
                ServiceLevel = dto.ServiceLevel,
                ContractType = dto.ContractType,
                EstimatedValueUsd = dto.EstimatedValueUsd,
                EstimatedValueZar = dto.EstimatedValueZar,
                AgreementFileName = dto.AgreementFileName,
                CreatedAt = dto.CreatedAt,
                StatusLogs = dto.StatusLogs?.Select(l => new ContractStatusLog
                {
                    Id = l.Id,
                    ContractId = dto.Id,
                    OldStatus = l.OldStatus,
                    NewStatus = l.NewStatus,
                    Department = l.Department,
                    ActionTaken = l.ActionTaken,
                    Timestamp = l.Timestamp
                }).ToList() ?? new List<ContractStatusLog>()
            };
        }
    }
}
