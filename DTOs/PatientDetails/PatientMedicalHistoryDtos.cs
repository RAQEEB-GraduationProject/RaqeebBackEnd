using System.ComponentModel.DataAnnotations;

namespace RAQEEB.DTOs.PatientDetails
{
    // Response of: GET /api/Patients/{patientId}/medical-history
    public class MedicalHistoryResponseDto
    {
        public List<MedicalHistoryItemDto> Diseases { get; set; } = new();

        public List<MedicalHistoryItemDto> Allergies { get; set; } = new();

        public List<MedicalHistoryItemDto> Surgeries { get; set; } = new();

        public List<MedicalHistoryItemDto> FamilyHistory { get; set; } = new();

        // Number of all active records (diseases + allergies + surgeries + family history)
        public int TotalCount { get; set; }
    }

    public class MedicalHistoryItemDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateOnly? Date { get; set; }

        public string? Notes { get; set; }
    }

    // Body of: POST /api/Patients/{patientId}/medical-history
    public class CreateMedicalHistoryEntryDto
    {
        [Required]
        [RegularExpression(@"^(?i)(Disease|Allergy|Surgery|FamilyHistory)$",
            ErrorMessage = "Type must be one of: Disease, Allergy, Surgery, FamilyHistory.")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        // Format: yyyy-MM-dd (e.g. 2025-02-01)
        public DateOnly? Date { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    // Response of the POST (the created record)
    public class MedicalHistoryEntryResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateOnly? Date { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
