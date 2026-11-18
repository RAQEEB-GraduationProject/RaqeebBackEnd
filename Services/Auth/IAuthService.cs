using RAQEEB.DTOs.Auth;

namespace RAQEEB.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);

        Task<RegisterResponseDto?> RegisterAsync(
            RegisterDto registerDto,
            Guid hospitalId);
    }
}