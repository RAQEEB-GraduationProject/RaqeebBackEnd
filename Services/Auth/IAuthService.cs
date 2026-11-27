using RAQEEB.DTOs.Auth;
using RAQEEB.DTOs.Common;

namespace RAQEEB.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginDto loginDto);

        Task<RegisterResult> RegisterAsync(
            RegisterDto registerDto,
            Guid hospitalId);

        // Returns an empty list on success, otherwise the reasons it failed.
        Task<List<ApiError>> ChangePasswordAsync(
            string userId,
            ChangePasswordDto dto);
    }
}
