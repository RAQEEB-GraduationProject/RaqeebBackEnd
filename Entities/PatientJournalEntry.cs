namespace RAQEEB.Entities
{
    public class PatientJournalEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // The patient this entry is for
        public Guid PatientId { get; set; }

        public Patient Patient { get; set; } = null!;



        // Instruction / Reminder / Note
        public JournalEntryType EntryType { get; set; }

        // e.g. "Drink plenty of water", "Take your medication on time"
        public string Content { get; set; } = string.Empty;



        // Who wrote the entry: Admin / Doctor now,
        // and possibly the Patient later from the mobile app.
        public string AuthorUserId { get; set; } = string.Empty;

        public ApplicationUser Author { get; set; } = null!;



        // Soft delete + audit
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
