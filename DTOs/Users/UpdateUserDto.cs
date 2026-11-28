using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RAQEEB.DTOs.Users
{
    // Body of: PUT /api/Users/{id}  (Admin, full replacement of the account details)
    // Same rule as PUT /api/Patients/{id}: every property must be present.
    //   missing property -> 400,  null / "" -> clear (fullName, phoneNumber only).
    // UserName, role, hospital and password are NOT changed here.
    public class UpdateUserDto
    {
        [JsonRequired]
        [StringLength(200)]
        public string? FullName { get; set; }

        // Email cannot be cleared: it must be a valid, unused address.
        [JsonRequired]
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [JsonRequired]
        [StringLength(30)]
        public string? PhoneNumber { get; set; }
    }
}
