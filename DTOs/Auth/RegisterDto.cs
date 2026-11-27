using System.ComponentModel.DataAnnotations;

namespace RAQEEB.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        // Same max length as the database column (200)
        [StringLength(200)]
        public string? FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        // Doctor, Nurse, Caregiver or Patient (checked in AuthService)
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
