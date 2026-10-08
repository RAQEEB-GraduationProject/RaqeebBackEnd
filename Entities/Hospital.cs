namespace RAQEEB.Entities
{
    public class Hospital
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ApplicationUser> Users { get; set; }
            = new List<ApplicationUser>();
    }
}