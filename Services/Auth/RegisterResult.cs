using RAQEEB.DTOs.Auth;
using RAQEEB.DTOs.Common;

namespace RAQEEB.Services.Auth
{
    public enum RegisterStatus
    {
        Success,
        UserNameTaken,
        EmailTaken,
        InvalidRole,
        InvalidData,            // Identity rejected the data (weak password, bad username, ...)
        RoleAssignmentFailed
    }

    // Result of IAuthService.RegisterAsync: either the new account (Success)
    // or the reason it failed, so the controller can tell the client what to fix.
    public class RegisterResult
    {
        public RegisterStatus Status { get; init; }

        public RegisterResponseDto? Data { get; init; }

        public List<ApiError> Errors { get; init; } = new();

        public static RegisterResult Succeeded(RegisterResponseDto data) =>
            new() { Status = RegisterStatus.Success, Data = data };

        public static RegisterResult Failed(
            RegisterStatus status,
            List<ApiError>? errors = null) =>
            new() { Status = status, Errors = errors ?? new List<ApiError>() };
    }
}
