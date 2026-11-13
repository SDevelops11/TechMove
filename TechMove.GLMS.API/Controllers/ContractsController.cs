using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.API.DTOs;
using TechMove.GLMS.API.Models;
using TechMove.GLMS.API.Repositories;
using TechMove.GLMS.API.Services;

namespace TechMove.GLMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ContractsController : ControllerBase
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

        /// <summary>
        /// Retrieves all freight contracts with optional LINQ filtering by date range, status, client, or search keyword.
        /// </summary>
        /// <response code="200">List of contracts matching filter criteria.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<ContractDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ContractDto>>> GetContracts(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] ContractStatus? status,
            [FromQuery] int? clientId,
            [FromQuery] string? searchTerm)
        {
            var contracts = await _contractService.SearchAndFilterContractsAsync(
                startDate, endDate, status, clientId, searchTerm);

            var dtos = contracts.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves a single freight contract by its unique database ID, including audit logs.
        /// </summary>
        /// <param name="id">The contract primary key.</param>
        /// <response code="200">Contract found.</response>
        /// <response code="404">Contract not found.</response>
        [HttpGet("{id:int}", Name = "GetContractById")]
        [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ContractDto>> GetById(int id)
        {
            var contract = await _contractService.GetContractDetailsAsync(id);
            if (contract == null)
            {
                return NotFound(new { message = $"Contract with ID {id} was not found." });
            }

            return Ok(MapToDto(contract));
        }

        /// <summary>
        /// Creates a new freight contract utilizing the GoF Factory Pattern to assign appropriate SLAs and multipliers.
        /// </summary>
        /// <param name="dto">Contract creation details.</param>
        /// <response code="201">Contract created successfully.</response>
        /// <response code="400">Validation failure (e.g., end date prior to start date, client does not exist).</response>
        [HttpPost]
        [ProducesResponseType(typeof(ContractDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ContractDto>> Create([FromBody] CreateContractRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.EndDate <= dto.StartDate)
            {
                return BadRequest(new { message = "Contract End Date must be strictly after the Start Date." });
            }

            var client = await _clientRepository.GetByIdAsync(dto.ClientId);
            if (client == null)
            {
                return BadRequest(new { message = $"Client with ID {dto.ClientId} does not exist." });
            }

            try
            {
                var createdContract = await _contractService.CreateContractAsync(
                    dto.ContractType.ToString(),
                    dto.ClientId,
                    dto.Title,
                    dto.StartDate,
                    dto.EndDate,
                    dto.BaseValueUsd,
                    agreementPdf: null);

                var resultDto = MapToDto(createdContract);
                return CreatedAtRoute("GetContractById", new { id = resultDto.Id }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create contract via API");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Updates the lifecycle status of a contract (PATCH verb) and triggers the GoF Observer Pattern.
        /// Broadcasts notifications to Operations, Finance, Client Services, and Compliance observers.
        /// </summary>
        /// <param name="id">Contract ID to modify.</param>
        /// <param name="dto">Target status and transition reason.</param>
        /// <response code="200">Status successfully updated and observers notified.</response>
        /// <response code="400">Invalid transition attempted or validation error.</response>
        /// <response code="404">Contract not found.</response>
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ContractDto>> UpdateStatus(int id, [FromBody] UpdateContractStatusRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var contract = await _contractService.GetContractDetailsAsync(id);
            if (contract == null)
            {
                return NotFound(new { message = $"Contract with ID {id} was not found." });
            }

            if (!_contractService.CanTransitionStatus(contract.Status, dto.NewStatus, out var transitionError))
            {
                return BadRequest(new { message = transitionError ?? "Invalid status transition." });
            }

            try
            {
                var success = await _contractService.UpdateContractStatusAsync(id, dto.NewStatus, dto.Reason);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to update contract status." });
                }

                var updatedContract = await _contractService.GetContractDetailsAsync(id);
                return Ok(MapToDto(updatedContract!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching contract status for ID {ContractId}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Uploads a signed PDF agreement for the designated contract. Enforces strict file validation (.pdf magic bytes).
        /// </summary>
        [HttpPost("{id:int}/agreement")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ContractDto>> UploadAgreement(int id, IFormFile file)
        {
            var contract = await _contractService.GetContractDetailsAsync(id);
            if (contract == null)
            {
                return NotFound(new { message = $"Contract with ID {id} was not found." });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file was attached to the upload request." });
            }

            if (!_fileStorageService.ValidatePdfFile(file, out var fileError))
            {
                return BadRequest(new { message = fileError ?? "Invalid PDF document." });
            }

            var success = await _contractService.UploadSignedAgreementAsync(id, file);
            if (!success)
            {
                return BadRequest(new { message = "Could not save the signed agreement to storage." });
            }

            var updatedContract = await _contractService.GetContractDetailsAsync(id);
            return Ok(MapToDto(updatedContract!));
        }

        /// <summary>
        /// Downloads the signed PDF agreement for the designated contract.
        /// </summary>
        [HttpGet("{id:int}/agreement")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            var contract = await _contractService.GetContractDetailsAsync(id);
            if (contract == null || string.IsNullOrWhiteSpace(contract.AgreementFilePath))
            {
                return NotFound(new { message = "No signed agreement exists for this contract." });
            }

            var fileData = _fileStorageService.GetFileForDownload(contract.AgreementFilePath, contract.AgreementFileName);
            if (fileData == null)
            {
                return NotFound(new { message = "The agreement file was not found on the file server." });
            }

            return File(fileData.Value.fileBytes, fileData.Value.contentType, fileData.Value.downloadName);
        }

        private static ContractDto MapToDto(Contract c)
        {
            return new ContractDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,
                Title = c.Title,
                ClientId = c.ClientId,
                ClientName = c.Client?.Name ?? $"Client #{c.ClientId}",
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                ServiceLevel = c.ServiceLevel,
                ContractType = c.ContractType,
                EstimatedValueUsd = c.EstimatedValueUsd,
                EstimatedValueZar = c.EstimatedValueZar,
                AgreementFileName = c.AgreementFileName,
                CreatedAt = c.CreatedAt,
                ActiveServiceRequestsCount = c.ServiceRequests?.Count ?? 0,
                StatusLogs = c.StatusLogs?.Select(l => new ContractStatusLogDto
                {
                    Id = l.Id,
                    OldStatus = l.OldStatus,
                    NewStatus = l.NewStatus,
                    Department = l.Department,
                    ActionTaken = l.ActionTaken,
                    Timestamp = l.Timestamp
                }).ToList() ?? new List<ContractStatusLogDto>()
            };
        }
    }
}
