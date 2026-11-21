using RAQEEB.DTOs.Diagnostics;

namespace RAQEEB.Services.Patients
{
    public enum DiagnosticFileStatus
    {
        Found,
        PatientNotFound,
        FileNotFound
    }

    public class DiagnosticFileResult
    {
        public string FullPath { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;
    }

    public interface IPatientDiagnosticsService
    {
        // Returns null when the patient does not exist
        // or the current user is not allowed to see it.
        Task<List<LabResultResponseDto>?> GetLabResultsAsync(Guid patientId);

        // Returns null when the patient does not exist
        // or the current user is not allowed to see it.
        Task<List<ImagingResponseDto>?> GetImagingAsync(Guid patientId);

        Task<(DiagnosticFileStatus Status, DiagnosticFileResult? File)>
            GetFileAsync(Guid patientId, Guid fileId);
    }
}
