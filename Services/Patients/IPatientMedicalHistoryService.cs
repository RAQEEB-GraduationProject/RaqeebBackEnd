using RAQEEB.DTOs.PatientDetails;

namespace RAQEEB.Services.Patients
{
    public enum CreateMedicalHistoryStatus
    {
        Created,
        PatientNotFound,
        PatientInactive
    }

    public interface IPatientMedicalHistoryService
    {
        // Returns null when the patient does not exist
        // or the current user is not allowed to see it.
        Task<MedicalHistoryResponseDto?> GetMedicalHistoryAsync(Guid patientId);

        Task<(CreateMedicalHistoryStatus Status, MedicalHistoryEntryResponseDto? Entry)>
            CreateEntryAsync(Guid patientId, CreateMedicalHistoryEntryDto dto);
    }
}
