namespace RAQEEB.DTOs.PatientAssignments
{
    // Item of: GET /api/PatientAssignments/Assignments
    // One patient with the staff currently assigned to them.
    // (Was DTOs/Patients/PatientOverviewDto - renamed so it is not confused
    //  with the patient details overview in DTOs/PatientDetails.)
    public class PatientStaffOverviewDto
    {
        public Guid PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string? PatientCode { get; set; }

        public List<AssignedStaffDto> AssignedDoctor { get; set; } = new();

        public List<AssignedStaffDto> AssignedNurses { get; set; } = new();

        public List<AssignedStaffDto> AssignedCaregivers { get; set; } = new();
    }

    public class AssignedStaffDto
    {
        public Guid AssignmentId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
