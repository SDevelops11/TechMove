using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Repositories;
using TechMove.GLMS.Web.Services;
using TechMove.GLMS.Web.ViewModels;

namespace TechMove.GLMS.Web.Controllers
{
    public class ContractsController : Controller
    {
        private readonly IContractService _contractService;
        private readonly IClientRepository _clientRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<ContractsController> _logger;

        public ContractsController(
            IContractService contractService,
            IClientRepository clientRepository,
            IFileStorageService fileStorageService,
            ILogger<ContractsController> logger)
        {
            _contractService = contractService;
            _clientRepository = clientRepository;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        // GET: Contracts
        // Demonstrates LINQ filtering by date range, status, client, and search term
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            ContractStatus? status,
            int? clientId,
            string? searchTerm)
        {
            var filteredContracts = await _contractService.SearchAndFilterContractsAsync(
                startDate, endDate, status, clientId, searchTerm);

            var clients = await _clientRepository.GetAllAsync();

            var model = new ContractIndexViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                ClientId = clientId,
                SearchTerm = searchTerm,
                Contracts = filteredContracts,
                ClientsList = new SelectList(clients, "Id", "Name", clientId)
            };

            return View(model);
        }

        // GET: Contracts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _contractService.GetContractDetailsAsync(id);
            if (contract == null)
            {
                TempData["ErrorMessage"] = "Contract not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(contract);
        }

        // GET: Contracts/Create
        public async Task<IActionResult> Create()
        {
            var clients = await _clientRepository.GetAllAsync();
            var model = new CreateContractViewModel
            {
                ClientsList = new SelectList(clients, "Id", "Name")
            };

            return View(model);
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
                if (!_fileStorageService.ValidatePdfFile(model.AgreementPdf, out var fileError))
                {
                    ModelState.AddModelError(nameof(model.AgreementPdf), fileError ?? "Invalid PDF agreement file.");
                }
            }

            if (!ModelState.IsValid)
            {
                var clients = await _clientRepository.GetAllAsync();
                model.ClientsList = new SelectList(clients, "Id", "Name", model.ClientId);
                return View(model);
            }

            try
            {
                var contract = await _contractService.CreateContractAsync(
                    model.ContractType,
                    model.ClientId,
                    model.Title,
                    model.StartDate,
                    model.EndDate,
                    model.BaseValueUsd,
                    model.AgreementPdf);

                TempData["SuccessMessage"] = $"Contract {contract.ContractNumber} created successfully via Factory Pattern!";
                return RedirectToAction(nameof(Details), new { id = contract.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract");
                ModelState.AddModelError(string.Empty, ex.Message);
                var clients = await _clientRepository.GetAllAsync();
                model.ClientsList = new SelectList(clients, "Id", "Name", model.ClientId);
                return View(model);
            }
        }

        // GET: Contracts/ChangeStatus/5
        public async Task<IActionResult> ChangeStatus(int id)
        {
            var contract = await _contractService.GetContractDetailsAsync(id);
            if (contract == null)
            {
                TempData["ErrorMessage"] = "Contract not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ChangeContractStatusViewModel
            {
                ContractId = contract.Id,
                ContractNumber = contract.ContractNumber,
                ContractTitle = contract.Title,
                CurrentStatus = contract.Status,
                NewStatus = contract.Status
            };

            return View(model);
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
                bool success = await _contractService.UpdateContractStatusAsync(
                    model.ContractId,
                    model.NewStatus,
                    model.Reason);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Contract status successfully updated to '{model.NewStatus}'. All 4 department observers were notified and logged.";
                    return RedirectToAction(nameof(Details), new { id = model.ContractId });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to update contract status.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        // GET: Contracts/DownloadAgreement/5
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            var contract = await _contractService.GetContractDetailsAsync(id);
            if (contract == null || string.IsNullOrWhiteSpace(contract.AgreementFilePath))
            {
                TempData["ErrorMessage"] = "No signed agreement is associated with this contract.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var fileData = _fileStorageService.GetFileForDownload(contract.AgreementFilePath, contract.AgreementFileName);
            if (fileData == null)
            {
                TempData["ErrorMessage"] = "Agreement file could not be found on server storage.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return File(fileData.Value.fileBytes, fileData.Value.contentType, fileData.Value.downloadName);
        }
    }
}
