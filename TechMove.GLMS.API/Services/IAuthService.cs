using TechMove.GLMS.API.DTOs;

namespace TechMove.GLMS.API.Services
{
    public interface IAuthService
    {
        AuthResponseDto? Authenticate(LoginRequestDto loginRequest);
        UserDto? GetUserByUsername(string username);
    }
}

