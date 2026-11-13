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
    public class ServiceRequestsController : ControllerBase
    {
        private readonly IServiceRequestService _requestService;
        private readonly IContractRepository _contractRepository;
        private readonly ILogger<ServiceRequestsController> _logger;

        public ServiceRequestsController(
            IServiceRequestService requestService,
            IContractRepository contractRepository,
            ILogger<ServiceRequestsController> logger)
        {
            _requestService = requestService;
            _contractRepository = contractRepository;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all logged haulage service requests across all contracts.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ServiceRequestDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ServiceRequestDto>>> GetAll()
        {
            var requests = await _requestService.GetAllRequestsAsync();
            var dtos = requests.Select(MapToDto).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves a single service request by its ID.
        /// </summary>
        [HttpGet("{id:int}", Name = "GetServiceRequestById")]
        [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceRequestDto>> GetById(int id)
        {
            var request = await _requestService.GetRequestByIdAsync(id);
            if (request == null)
            {
                return NotFound(new { message = $"Service request with ID {id} was not found." });
            }

            return Ok(MapToDto(request));
        }

        /// <summary>
        /// Retrieves all service requests associated with a particular contract.
        /// </summary>
        [HttpGet("by-contract/{contractId:int}")]
        [ProducesResponseType(typeof(List<ServiceRequestDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ServiceRequestDto>>> GetByContract(int contractId)
        {
            var requests = await _requestService.GetRequestsByContractAsync(contractId);
            return Ok(requests.Select(MapToDto).ToList());
        }

        /// <summary>
        /// Creates a new service request against an active freight contract.
        /// WORKFLOW RULE: Strictly forbids raising requests against 'Expired' or 'On Hold' contracts.
        /// Converts international currency (USD/EUR/GBP) to local ZAR using the GoF Strategy Pattern and live exchange rates.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ServiceRequestDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ServiceRequestDto>> Create([FromBody] CreateServiceRequestRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Enforce parent contract workflow rule
            var contract = await _contractRepository.GetByIdAsync(dto.ContractId);
            if (contract == null)
            {
                return BadRequest(new { message = $"Contract with ID {dto.ContractId} does not exist." });
            }

            if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
            {
                _logger.LogWarning("Workflow violation rejected: Attempted service request on {ContractNumber} with status {Status}",
                    contract.ContractNumber, contract.Status);

                return BadRequest(new
                {
                    message = $"Workflow Policy Violation: Contract '{contract.ContractNumber}' is currently in '{contract.Status}' status. " +
                              $"Service Requests cannot be raised against Expired or On Hold contracts."
                });
            }

            try
            {
                var internalDto = new CreateServiceRequestDto
                {
                    ContractId = dto.ContractId,
                    Description = dto.Description,
                    Origin = dto.Origin,
                    Destination = dto.Destination,
                    Currency = dto.Currency,
                    Amount = dto.Amount
                };

                var createdRequest = await _requestService.CreateServiceRequestAsync(internalDto);
                var resultDto = MapToDto(createdRequest);

                return CreatedAtRoute("GetServiceRequestById", new { id = resultDto.Id }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create service request via API");
                return BadRequest(new { message = ex.Message });
            }
        }

        private static ServiceRequestDto MapToDto(ServiceRequest r)
        {
            return new ServiceRequestDto
            {
                Id = r.Id,
                RequestNumber = r.RequestNumber,
                ContractId = r.ContractId,
                ContractNumber = r.Contract?.ContractNumber ?? $"Contract #{r.ContractId}",
                ContractTitle = r.Contract?.Title ?? string.Empty,
                ClientName = r.Contract?.Client?.Name ?? string.Empty,
                Description = r.Description,
                Origin = r.Origin,
                Destination = r.Destination,
                Cost = r.Cost,
                OriginalCostUsd = r.OriginalCostUsd,
                OriginalCurrency = r.OriginalCurrency,
                ExchangeRateUsed = r.ExchangeRateUsed,
                Status = r.Status,
                CreatedDate = r.CreatedDate
            };
        }
    }
}
