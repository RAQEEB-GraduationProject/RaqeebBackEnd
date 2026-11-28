using RAQEEB.DTOs.Patients;

namespace RAQEEB.DTOs.PatientDetails
{
    // Response of: GET /api/Patients/{patientId}/overview
    public class PatientOverviewResponseDto
    {
        // ---------- NEW structure (use these) ----------

        // Same object and same data source as GET /api/Patients and GET /api/Patients/{id}.
        public PatientResponseDto Patient { get; set; } = new();

        // Active care team of the patient (from Patient Assignments).
        public PatientCareTeamDto CareTeam { get; set; } = new();

        // Null until the Treatment / Chemotherapy module exists (no fake data).
        public TreatmentProgressDto? TreatmentProgress { get; set; }

        // ---------- OLD structure (deprecated) ----------
        // Kept only so the current frontend keeps working.
        // Remove after every screen reads "patient" / "careTeam" instead.

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

    // ---------- New overview parts ----------

    public class PatientCareTeamDto
    {
        // Earliest active assignment of each role.
        public CareTeamMemberDto? TreatingDoctor { get; set; }

        public CareTeamMemberDto? Nurse { get; set; }

        public CareTeamMemberDto? PrimaryCaregiver { get; set; }
    }

    public class CareTeamMemberDto
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        // As stored (null if not set). Display rule: fullName -> userName.
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

        public string Role { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; }
    }

    // Filled by the Treatment / Chemotherapy module later (TreatmentCycle in the book).
    public class TreatmentProgressDto
    {
        public string? TreatmentStatus { get; set; }

        public int? CurrentCycle { get; set; }

        public int? TotalCycles { get; set; }

        public int? CurrentDay { get; set; }
    }
}
