using RAQEEB.DTOs.Auth;

namespace RAQEEB.Services.Auth
{
    public enum LoginStatus
    {
        Success,
        InvalidCredentials,   // wrong username / password, or inactive account
        LockedOut             // too many wrong passwords in a row
    }

    public class LoginResult
    {
        public LoginStatus Status { get; init; }

        public LoginResponseDto? Data { get; init; }

        public static LoginResult Succeeded(LoginResponseDto data) =>
            new() { Status = LoginStatus.Success, Data = data };

        public static LoginResult Failed(LoginStatus status) =>
            new() { Status = status };
    }
}
