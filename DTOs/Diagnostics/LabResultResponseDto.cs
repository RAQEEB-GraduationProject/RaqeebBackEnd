namespace RAQEEB.DTOs.Diagnostics
{
    // Item of: GET /api/Patients/{patientId}/diagnostics/labs
    public class LabResultResponseDto
    {
        public Guid LabResultId { get; set; }

        public Guid PatientId { get; set; }

        public string TestName { get; set; } = string.Empty;

        public string ResultValue { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public string? Flag { get; set; }

        public DateTime RecordedAt { get; set; }

        public string? FileName { get; set; }

        public string? ContentType { get; set; }

        // Authenticated endpoint: /api/Patients/{patientId}/diagnostics/files/{fileId}
        public string? FileUrl { get; set; }
    }
}
