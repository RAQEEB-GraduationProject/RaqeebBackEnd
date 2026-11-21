using RAQEEB.DTOs.PatientDetails;

namespace RAQEEB.Services.Patients
{
    public enum CreateAppointmentStatus
    {
        Created,
        PatientNotFound,
        PatientInactive,
        InvalidProvider
    }

    public interface IPatientAppointmentsService
    {
        // Returns null when the patient does not exist
        // or the current user is not allowed to see it.
        // upcomingOnly = true returns only today's and future appointments.
        Task<AppointmentsResponseDto?> GetAppointmentsAsync(
            Guid patientId,
            bool upcomingOnly);

        Task<(CreateAppointmentStatus Status, AppointmentItemDto? Appointment)>
            CreateAppointmentAsync(Guid patientId, CreateAppointmentDto dto);
    }
}
