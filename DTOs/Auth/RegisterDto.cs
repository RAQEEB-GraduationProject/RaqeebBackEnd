namespace RAQEEB.DTOs.Auth
{
    public class RegisterDto // basic DTO test for user registration
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}
