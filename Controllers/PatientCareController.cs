using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAQEEB.DTOs.Common;
using RAQEEB.DTOs.PatientDetails;
using RAQEEB.Services.Patients;

namespace RAQEEB.Controllers
{
    [ApiController]
    [Route("api/Patients/{patientId:guid}")]
    [Authorize]
    public class PatientCareController : ControllerBase
    {
        private readonly IPatientJournalService _journalService;
        private readonly IPatientAppointmentsService _appointmentsService;

        public PatientCareController(
            IPatientJournalService journalService,
            IPatientAppointmentsService appointmentsService)
        {
            _journalService = journalService;
            _appointmentsService = appointmentsService;
        }

        // ---------- Journal ----------

        // GET: api/Patients/{patientId}/journal
        [HttpGet("journal")]
        public async Task<IActionResult> GetJournal(Guid patientId)
        {
            var result = await _journalService.GetJournalAsync(patientId);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<JournalResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            return Ok(
                ApiResponse<JournalResponseDto>.SuccessResponse(
                    result,
                    "Journal retrieved successfully."
                )
            );
        }

        // POST: api/Patients/{patientId}/journal
        // Admin: any patient in the hospital.
        // Doctor: only patients assigned to them.
        [HttpPost("journal")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> AddJournalEntry(
            Guid patientId,
            [FromBody] CreateJournalEntryDto dto)
        {
            var (status, entry) = await _journalService
                .CreateEntryAsync(patientId, dto);

            if (status == CreateJournalEntryStatus.PatientNotFound)
            {
                return NotFound(
                    ApiResponse<JournalEntryItemDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            if (status == CreateJournalEntryStatus.PatientInactive)
            {
                return Conflict(
                    ApiResponse<JournalEntryItemDto>.FailureResponse(
                        "PATIENT_INACTIVE",
                        "Cannot add a journal entry to an inactive patient."
                    )
                );
            }

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<JournalEntryItemDto>.SuccessResponse(
                    entry!,
                    "Journal entry added successfully."
                )
            );
        }

        // ---------- Appointments ----------

        // GET: api/Patients/{patientId}/appointments
        // GET: api/Patients/{patientId}/appointments?upcoming=true
        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments(
            Guid patientId,
            [FromQuery] bool upcoming = false)
        {
            var result = await _appointmentsService
                .GetAppointmentsAsync(patientId, upcoming);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<AppointmentsResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            return Ok(
                ApiResponse<AppointmentsResponseDto>.SuccessResponse(
                    result,
                    "Appointments retrieved successfully."
                )
            );
        }

        // POST: api/Patients/{patientId}/appointments
        // Admin: any patient in the hospital.
        // Doctor: only patients assigned to them.
        [HttpPost("appointments")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> AddAppointment(
            Guid patientId,
            [FromBody] CreateAppointmentDto dto)
        {
            var (status, appointment) = await _appointmentsService
                .CreateAppointmentAsync(patientId, dto);

            if (status == CreateAppointmentStatus.PatientNotFound)
            {
                return NotFound(
                    ApiResponse<AppointmentItemDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            if (status == CreateAppointmentStatus.PatientInactive)
            {
                return Conflict(
                    ApiResponse<AppointmentItemDto>.FailureResponse(
                        "PATIENT_INACTIVE",
                        "Cannot add an appointment to an inactive patient."
                    )
                );
            }

            if (status == CreateAppointmentStatus.InvalidProvider)
            {
                return BadRequest(
                    ApiResponse<AppointmentItemDto>.FailureResponse(
                        "INVALID_PROVIDER",
                        "The provider must be an active Doctor in your hospital.",
                        new List<ApiError>
                        {
                            new ApiError
                            {
                                Field = "ProviderId",
                                Message = "The provider must be an active Doctor in your hospital."
                            }
                        }
                    )
                );
            }

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<AppointmentItemDto>.SuccessResponse(
                    appointment!,
                    "Appointment added successfully."
                )
            );
        }
    }
}
