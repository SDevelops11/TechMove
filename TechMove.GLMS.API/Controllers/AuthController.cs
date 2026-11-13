using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.API.DTOs;
using TechMove.GLMS.API.Services;

namespace TechMove.GLMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a logistics user and returns a signed JWT bearer token.
        /// </summary>
        /// <param name="request">Login credentials (username/email and password).</param>
        /// <returns>Auth token, role, and expiration details.</returns>
        /// <response code="200">Authentication successful; JWT token returned.</response>
        /// <response code="400">Request validation failed or missing parameters.</response>
        /// <response code="401">Invalid username or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = _authService.Authenticate(request);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password. Please verify your credentials." });
            }

            _logger.LogInformation("User {Username} successfully logged in via API", request.Username);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the current authenticated user profile from the JWT claims.
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetCurrentUser()
        {
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized();
            }

            var user = _authService.GetUserByUsername(username);
            if (user == null)
            {
                return NotFound(new { message = "User record not found." });
            }

            return Ok(user);
        }
    }
}

