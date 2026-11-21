using RAQEEB.DTOs.PatientDetails;

namespace RAQEEB.Services.Patients
{
    public interface IPatientOverviewService
    {
        // Returns null when the patient does not exist
        // or the current user is not allowed to see it.
        Task<PatientOverviewResponseDto?> GetOverviewAsync(Guid patientId);
    }
}
