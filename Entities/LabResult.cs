namespace RAQEEB.Entities
{
    public class LabResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // The patient this result belongs to
        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;



        // e.g. "Hemoglobin", "13.5", "g/dL", "Normal" / "High" / "Low"
        public string TestName { get; set; } = string.Empty;

        public string ResultValue { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public string? Flag { get; set; }

        public DateTime RecordedAt { get; set; }



        // Optional attached report (PDF)
        public Guid? DiagnosticFileId { get; set; }

        public DiagnosticFile? DiagnosticFile { get; set; }



        // Soft delete + audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
