namespace RAQEEB.DTOs.Users
{
    // Returned by GET /api/Users, GET /api/Users/{id} and PUT /api/Users/{id}.
    // Values are returned as stored (empty text = null). No field replaces another,
    // so the same object can fill both the display and the edit form.
    // Display name rule is the frontend's job: fullName -> userName -> email.
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        public Guid HospitalId { get; set; }

        public bool IsActive { get; set; }
    }
}
