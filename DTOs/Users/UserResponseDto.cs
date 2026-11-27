namespace RAQEEB.DTOs.Users
{
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        // Full name (falls back to the username when empty).
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public Guid HospitalId { get; set; }

        public bool IsActive { get; set; }
    }
}
