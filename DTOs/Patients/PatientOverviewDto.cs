namespace RAQEEB.DTOs.Patients
{
    public class PatientOverviewDto
    {
        public Guid PatientId { get; set; }

        public string PatientName { get; set; } = string.Empty;

        public string? PatientCode { get; set; }

        public List<AssignedStaffDto> AssignedDoctor { get; set; } = new();

        public List<AssignedStaffDto> AssignedNurses { get; set; } = new();

        public List<AssignedStaffDto> AssignedCaregivers { get; set; } = new();

        public string Status { get; set; } = string.Empty;
    }

    public class AssignedStaffDto
    {
        public string UserId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}