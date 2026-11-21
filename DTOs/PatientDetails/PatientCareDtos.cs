using System.ComponentModel.DataAnnotations;

namespace RAQEEB.DTOs.PatientDetails
{
    // ---------- Journal ----------

    // Response of: GET /api/Patients/{patientId}/journal
    public class JournalResponseDto
    {
        public List<JournalEntryItemDto> Entries { get; set; } = new();

        public int TotalCount { get; set; }
    }

    public class JournalEntryItemDto
    {
        public Guid EntryId { get; set; }

        public string EntryType { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public JournalAuthorDto Author { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }

    public class JournalAuthorDto
    {
        public string UserId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Role { get; set; }
    }

    // Body of: POST /api/Patients/{patientId}/journal
    public class CreateJournalEntryDto
    {
        [Required]
        [RegularExpression(@"^(?i)(Instruction|Reminder|Note)$",
            ErrorMessage = "EntryType must be one of: Instruction, Reminder, Note.")]
        public string EntryType { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    // ---------- Appointments ----------

    // Response of: GET /api/Patients/{patientId}/appointments
    public class AppointmentsResponseDto
    {
        public List<AppointmentItemDto> Appointments { get; set; } = new();

        public int TotalCount { get; set; }
    }

    public class AppointmentItemDto
    {
        public Guid AppointmentId { get; set; }

        public string Title { get; set; } = string.Empty;

        // Format: yyyy-MM-dd
        public DateOnly AppointmentDate { get; set; }

        // Format: HH:mm:ss
        public TimeOnly AppointmentTime { get; set; }

        public AppointmentProviderDto Provider { get; set; } = new();

        public string? Location { get; set; }
    }

    public class AppointmentProviderDto
    {
        public string DoctorId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    // Body of: POST /api/Patients/{patientId}/appointments
    public class CreateAppointmentDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        // Format: yyyy-MM-dd (past dates are allowed)
        [Required]
        public DateOnly? AppointmentDate { get; set; }

        // Format: HH:mm:ss
        [Required]
        public TimeOnly? AppointmentTime { get; set; }

        // The user id of a Doctor in the same hospital
        [Required]
        public string ProviderId { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Location { get; set; }
    }
}
