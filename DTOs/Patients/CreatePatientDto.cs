namespace RAQEEB.DTOs.Patients
{
    public class CreatePatientDto
    {
        public string UserId { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? MedicalRecordNumber { get; set; }
    }
}