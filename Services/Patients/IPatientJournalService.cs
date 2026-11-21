using RAQEEB.DTOs.PatientDetails;

namespace RAQEEB.Services.Patients
{
    public enum CreateJournalEntryStatus
    {
        Created,
        PatientNotFound,
        PatientInactive
    }

    public interface IPatientJournalService
    {
        // Returns null when the patient does not exist
        // or the current user is not allowed to see it.
        Task<JournalResponseDto?> GetJournalAsync(Guid patientId);

        Task<(CreateJournalEntryStatus Status, JournalEntryItemDto? Entry)>
            CreateEntryAsync(Guid patientId, CreateJournalEntryDto dto);
    }
}
