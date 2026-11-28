using System.ComponentModel.DataAnnotations;
using RAQEEB.Common.Validation;
namespace RAQEEB.DTOs.Patients
{
    // Body of: POST /api/Patients  (step 2 of registration)
    // Only userId is required. Missing optional fields are saved as null
    // (medicalRecordNumber stays null until a real number exists).
    public class CreatePatientDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [NotInFuture]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(30)]
        public string? Gender { get; set; }

        [StringLength(100)]
        public string? MedicalRecordNumber { get; set; }
        
        [StringLength(200)]
        public string? Diagnosis { get; set; }

        [StringLength(100)]
        public string? Stage { get; set; }

        [StringLength(200)]
        public string? Treatment { get; set; }

        [StringLength(200)]
        public string? Protocol { get; set; }

        [StringLength(50)]
        public string? RiskLevel { get; set; }

        [RegularExpression(@"^\s*(A|B|AB|O)[+-]\s*$",
            ErrorMessage = "Blood group must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")]
        public string? BloodGroup { get; set; }
    }
}