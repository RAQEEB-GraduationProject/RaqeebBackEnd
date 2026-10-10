namespace RAQEEB.Entities
{
    public class PatientAssignment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // The patient receiving care
        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;



        // This stores the ID of the healthcare professional assigned to the patient.
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;



        // Assignment information
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}