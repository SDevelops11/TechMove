using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TechMove.GLMS.API.DTOs;

namespace TechMove.GLMS.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        // In enterprise production, users are stored in the database with salted hashes.
        // For GLMS enterprise demonstration and grading testing, we provide standard role-based accounts:
        private static readonly List<(string Username, string Password, string FullName, string Role, string Email)> DemoUsers = new()
        {
            ("admin@techmove.com", "Admin@123", "Chief Solutions Architect", "Admin", "admin@techmove.com"),
            ("manager@techmove.com", "Manager@123", "Logistics Operations Lead", "LogisticsManager", "manager@techmove.com"),
            ("operator@techmove.com", "Operator@123", "Haulage Coordinator", "LogisticsOperator", "operator@techmove.com")
        };

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public AuthResponseDto? Authenticate(LoginRequestDto loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                return null;
            }

            var user = DemoUsers.FirstOrDefault(u =>
                u.Username.Equals(loginRequest.Username.Trim(), StringComparison.OrdinalIgnoreCase) &&
                u.Password == loginRequest.Password);

            if (user == default)
            {
                _logger.LogWarning("Authentication failed for username '{Username}'", loginRequest.Username);
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? "TechMove_Enterprise_GLMS_Super_Secret_Key_2026_Secure_Token_Must_Be_Long_Enough!";
            var key = Encoding.UTF8.GetBytes(secretKey);
            var issuer = _configuration["JwtSettings:Issuer"] ?? "TechMove.GLMS.API";
            var audience = _configuration["JwtSettings:Audience"] ?? "TechMove.GLMS.Clients";
            var expirationMinutes = int.TryParse(_configuration["JwtSettings:ExpirationInMinutes"], out var exp) ? exp : 120;
            var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Username),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("Department", "GlobalLogistics")
                }),
                Expires = expires,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("JWT token issued successfully for user '{Username}' [Role: {Role}]", user.Username, user.Role);

            return new AuthResponseDto
            {
                Token = tokenString,
                Expiration = expires,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        public UserDto? GetUserByUsername(string username)
        {
            var user = DemoUsers.FirstOrDefault(u => u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
            if (user == default) return null;

            return new UserDto
            {
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                Email = user.Email
            };
        }
    }
}

