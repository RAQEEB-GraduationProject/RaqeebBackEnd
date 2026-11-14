namespace RAQEEB.DTOs.Patients
{

    // THIS IS PATIENT ASSIGNMENT STUFF DTO, NOT PATIENT DTO. IT IS USED TO RETURN ASSIGNED STAFF FOR A PATIENT.
    public class PatientOverviewDto
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