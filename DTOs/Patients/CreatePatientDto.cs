using System.ComponentModel.DataAnnotations;
namespace RAQEEB.DTOs.Patients
{
    public class CreatePatientDto
    {
        public string UserId { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? MedicalRecordNumber { get; set; }
        
        public string? Diagnosis { get; set; }

        public string? Stage { get; set; }

        public string? Treatment { get; set; }

        public string? Protocol { get; set; }

        public string? RiskLevel { get; set; }

        [RegularExpression(@"^(A|B|AB|O)[+-]$",
            ErrorMessage = "Blood group must be one of: A+, A-, B+, B-, AB+, AB-, O+, O-.")]
        public string? BloodGroup { get; set; }
    }
}