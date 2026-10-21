namespace RAQEEB.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime Expiration { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public Guid HospitalId { get; set; }

        public IList<string> Roles { get; set; } = new List<string>();

        public Guid? PatientId { get; set; }
    }
}