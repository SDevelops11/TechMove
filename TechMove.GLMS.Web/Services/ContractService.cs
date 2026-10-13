using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TechMove.GLMS.Web.Data;
using TechMove.GLMS.Web.Factories;
using TechMove.GLMS.Web.Models;
using TechMove.GLMS.Web.Observers;
using TechMove.GLMS.Web.Repositories;

namespace TechMove.GLMS.Web.Services
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IContractFactory _contractFactory;
        private readonly IContractNotifier _contractNotifier;
        private readonly IFileStorageService _fileStorageService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ContractService> _logger;

        public ContractService(
            IContractRepository contractRepository,
            IContractFactory contractFactory,
            IContractNotifier contractNotifier,
            IFileStorageService fileStorageService,
            IExchangeRateService exchangeRateService,
            AppDbContext dbContext,
            ILogger<ContractService> logger)
        {
            _contractRepository = contractRepository;
            _contractFactory = contractFactory;
            _contractNotifier = contractNotifier;
            _fileStorageService = fileStorageService;
            _exchangeRateService = exchangeRateService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<Contract>> GetAllContractsAsync()
        {
            return await _contractRepository.GetAllAsync();
        }

        public async Task<List<Contract>> SearchAndFilterContractsAsync(
            DateTime? startDate,
            DateTime? endDate,
            ContractStatus? status,
            int? clientId,
            string? searchTerm)
        {
            return await _contractRepository.FilterContractsAsync(startDate, endDate, status, clientId, searchTerm);
        }

        public async Task<Contract?> GetContractDetailsAsync(int id)
        {
            return await _contractRepository.GetByIdAsync(id, includeDetails: true);
        }

        public async Task<Contract> CreateContractAsync(
            string contractType,
            int clientId,
            string title,
            DateTime startDate,
            DateTime endDate,
            decimal baseValueUsd,
            IFormFile? agreementPdf)
        {
            if (endDate <= startDate)
            {
                throw new ArgumentException("Contract end date must be strictly after the start date.");
            }

            // 1. Get live USD to ZAR exchange rate for financial estimates
            var fxRate = await _exchangeRateService.GetExchangeRateAsync("USD", "ZAR");

            // 2. Utilize Factory Pattern to instantiate the proper Contract entity
            var contract = _contractFactory.CreateEntity(
                contractType,
                clientId,
                title,
                startDate,
                endDate,
                baseValueUsd,
                fxRate);

            // 3. Handle PDF file upload if provided
            if (agreementPdf != null && agreementPdf.Length > 0)
            {
                var uploadResult = await _fileStorageService.SaveContractAgreementPdfAsync(agreementPdf);
                if (!uploadResult.IsSuccess)
                {
                    throw new InvalidOperationException($"PDF Upload failed: {uploadResult.ErrorMessage}");
                }

                contract.AgreementFileName = uploadResult.OriginalFileName;
                contract.AgreementFilePath = uploadResult.RelativePath;
                contract.AgreementFileSizeBytes = uploadResult.FileSizeBytes;
                contract.AgreementUploadedAt = DateTime.UtcNow;
            }

            // 4. Save to database via repository
            await _contractRepository.AddAsync(contract);
            _logger.LogInformation("Successfully created contract {ContractNumber} for client {ClientId}",
                contract.ContractNumber, clientId);

            return contract;
        }

        public bool CanTransitionStatus(ContractStatus currentStatus, ContractStatus newStatus, out string? errorMessage)
        {
            errorMessage = null;

            if (currentStatus == newStatus)
            {
                errorMessage = $"Contract is already in '{currentStatus}' status.";
                return false;
            }

            // Business Rules Matrix:
            // Draft -> Active, OnHold, Expired
            // Active -> OnHold, Expired
            // OnHold -> Active, Expired
            // Expired -> Terminal (cannot transition)
            if (currentStatus == ContractStatus.Expired)
            {
                errorMessage = "An Expired contract cannot change status. A new renewal contract must be drafted.";
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateContractStatusAsync(int contractId, ContractStatus newStatus, string? reason = null)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId, includeDetails: false);
            if (contract == null)
            {
                _logger.LogWarning("Contract {Id} not found for status update", contractId);
                return false;
            }

            var oldStatus = contract.Status;

            if (!CanTransitionStatus(oldStatus, newStatus, out var error))
            {
                throw new InvalidOperationException(error);
            }

            // Update status
            contract.Status = newStatus;
            await _contractRepository.UpdateAsync(contract);

            // Trigger Observer Pattern to notify Operations, Finance, Client Services, Compliance
            var statusLogs = await _contractNotifier.NotifyAsync(contract, oldStatus, newStatus, reason);

            if (statusLogs.Count > 0)
            {
                await _dbContext.ContractStatusLogs.AddRangeAsync(statusLogs);
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("Contract {ContractNumber} transitioned from {OldStatus} to {NewStatus}",
                contract.ContractNumber, oldStatus, newStatus);

            return true;
        }

        public async Task<bool> UploadSignedAgreementAsync(int contractId, IFormFile agreementPdf)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId);
            if (contract == null) return false;

            var uploadResult = await _fileStorageService.SaveContractAgreementPdfAsync(agreementPdf);
            if (!uploadResult.IsSuccess)
            {
                throw new InvalidOperationException(uploadResult.ErrorMessage);
            }

            contract.AgreementFileName = uploadResult.OriginalFileName;
            contract.AgreementFilePath = uploadResult.RelativePath;
            contract.AgreementFileSizeBytes = uploadResult.FileSizeBytes;
            contract.AgreementUploadedAt = DateTime.UtcNow;

            await _contractRepository.UpdateAsync(contract);
            return true;
        }
    }
}
