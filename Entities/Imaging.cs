namespace RAQEEB.Entities
{
    public class Imaging
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // The patient this study belongs to
        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;



        // e.g. "Chest CT", "Breast MRI"
        public string StudyName { get; set; } = string.Empty;

        public DateTime StudyDate { get; set; }

        public string? Finding { get; set; }



        // Optional attached report (PDF)
        public Guid? DiagnosticFileId { get; set; }

        public DiagnosticFile? DiagnosticFile { get; set; }



        // Soft delete + audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
