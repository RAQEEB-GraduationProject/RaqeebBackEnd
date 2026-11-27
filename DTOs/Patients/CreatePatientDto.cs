using System.ComponentModel.DataAnnotations;
namespace RAQEEB.DTOs.Patients
{
    public class CreatePatientDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

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

        [RegularExpression(@"^(A|B|AB|O)[+-]$",
            ErrorMessage = "Blood group must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")]
        public string? BloodGroup { get; set; }
    }
}