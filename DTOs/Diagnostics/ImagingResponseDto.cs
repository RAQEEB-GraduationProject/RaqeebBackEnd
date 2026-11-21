namespace RAQEEB.DTOs.Diagnostics
{
    // Item of: GET /api/Patients/{patientId}/diagnostics/imaging
    public class ImagingResponseDto
    {
        public Guid ImagingId { get; set; }

        public Guid PatientId { get; set; }

        public string StudyName { get; set; } = string.Empty;

        public DateTime StudyDate { get; set; }

        public string? Finding { get; set; }

        public string? FileName { get; set; }

        public string? ContentType { get; set; }

        // Authenticated endpoint: /api/Patients/{patientId}/diagnostics/files/{fileId}
        public string? FileUrl { get; set; }
    }
}
