using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.API.DTOs;
using TechMove.GLMS.API.Models;
using TechMove.GLMS.API.Repositories;

namespace TechMove.GLMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(IClientRepository clientRepository, ILogger<ClientsController> logger)
        {
            _clientRepository = clientRepository;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all registered enterprise shipping clients.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClientDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ClientDto>>> GetAll()
        {
            var clients = await _clientRepository.GetAllAsync();
            var dtos = clients.Select(c => new ClientDto
            {
                Id = c.Id,
                Name = c.Name,
                ContactPerson = c.ContactPerson,
                ContactEmail = c.ContactEmail,
                ContactPhone = c.ContactPhone,
                Region = c.Region,
                CreatedAt = c.CreatedAt,
                ContractsCount = c.Contracts?.Count ?? 0
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves a specific shipping client by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClientDto>> GetById(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = $"Client with ID {id} was not found." });
            }

            return Ok(new ClientDto
            {
                Id = client.Id,
                Name = client.Name,
                ContactPerson = client.ContactPerson,
                ContactEmail = client.ContactEmail,
                ContactPhone = client.ContactPhone,
                Region = client.Region,
                CreatedAt = client.CreatedAt,
                ContractsCount = client.Contracts?.Count ?? 0
            });
        }
    }
}
