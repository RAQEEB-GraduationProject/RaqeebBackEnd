namespace RAQEEB.DTOs.Patients
{
    public class UpdatePatientDto
    {
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? MedicalRecordNumber { get; set; }

        public string? Diagnosis { get; set; }

        public string? Stage { get; set; }

        public string? Treatment { get; set; }

        public string? Protocol { get; set; }

        public string? RiskLevel { get; set; }
    }
}