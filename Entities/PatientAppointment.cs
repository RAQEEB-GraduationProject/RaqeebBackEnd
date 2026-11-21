namespace RAQEEB.Entities
{
    public class PatientAppointment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // The patient this appointment is for
        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;



        public string Title { get; set; } = string.Empty;

        // Stored as local (Egypt) date and time, without a time zone
        public DateOnly AppointmentDate { get; set; }

        public TimeOnly AppointmentTime { get; set; }

        public string? Location { get; set; }



        // The doctor of the appointment (must be a Doctor in the same hospital)
        public string ProviderUserId { get; set; } = string.Empty;

        public ApplicationUser Provider { get; set; } = null!;



        // Who created the appointment (Admin or Doctor)
        public string CreatedByUserId { get; set; } = string.Empty;

        public ApplicationUser CreatedBy { get; set; } = null!;



        // Soft delete + audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
