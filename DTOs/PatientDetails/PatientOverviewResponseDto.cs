namespace RAQEEB.DTOs.PatientDetails
{
    // Response of: GET /api/Patients/{patientId}/overview
    public class PatientOverviewResponseDto
    {
        public PatientHeaderDto PatientHeader { get; set; } = new();

        public PatientClinicalOverviewDto ClinicalOverview { get; set; } = new();

        public PatientPersonalInfoDto PersonalInfo { get; set; } = new();
    }

    // Shared header that the frontend keeps while navigating between patient pages
    public class PatientHeaderDto
    {
        public Guid PatientId { get; set; }

        public string? PatientCode { get; set; }

        public string FullName { get; set; } = string.Empty;

        public int? Age { get; set; }

        public string? Gender { get; set; }

        public string? BloodGroup { get; set; }

        public string? RiskLevel { get; set; }
    }

    public class PatientPersonalInfoDto
    {
        public string? Phone { get; set; }

        public string? PrimaryCaregiver { get; set; }
    }

    public class PatientClinicalOverviewDto
    {
        public string? CancerType { get; set; }

        public string? Stage { get; set; }

        public TreatingDoctorDto? TreatingDoctor { get; set; }

        // These four will be filled from the Chemotherapy module (Module 2).
        // They are null for now so the JSON shape stays stable.
        public string? TreatmentStatus { get; set; }

        public string? Protocol { get; set; }

        public int? CurrentCycle { get; set; }

        public int? TotalCycles { get; set; }

        public int? CurrentDay { get; set; }
    }

    public class TreatingDoctorDto
    {
        public string DoctorId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
