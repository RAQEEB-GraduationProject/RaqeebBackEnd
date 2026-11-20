namespace RAQEEB.Entities
{
    public class PatientMedicalHistoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // The patient this record belongs to
        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;



        // Disease / Allergy / Surgery / FamilyHistory
        public MedicalHistoryType Type { get; set; }

        // e.g. "Controlled hypertension", "Penicillin", "Lumpectomy"
        public string Name { get; set; } = string.Empty;

        // Mainly used for surgeries (the frontend decides how to display it, e.g. "Feb 2025")
        public DateOnly? EventDate { get; set; }

        public string? Notes { get; set; }



        // Soft delete + audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
