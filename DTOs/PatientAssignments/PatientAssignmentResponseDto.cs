namespace RAQEEB.DTOs.PatientAssignments
{
    public class PatientAssignmentResponseDto
    {
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime AssignedAt { get; set; }

        public bool IsActive { get; set; }
    }
}