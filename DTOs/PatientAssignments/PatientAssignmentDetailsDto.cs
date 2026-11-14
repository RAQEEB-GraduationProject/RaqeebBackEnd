namespace RAQEEB.DTOs.PatientAssignments
{
    public class PatientAssignmentDetailsDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string PatientUserName { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string AssignedUserName { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; }

        public bool IsActive { get; set; }
    }
}