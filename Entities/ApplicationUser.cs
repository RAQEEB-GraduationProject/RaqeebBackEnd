using Microsoft.AspNetCore.Identity;

namespace RAQEEB.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public Guid HospitalId { get; set; }

        public Hospital Hospital { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}