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
    public class PatientMedicalHistoryController : ControllerBase
    {
        private readonly IPatientMedicalHistoryService _medicalHistoryService;

        public PatientMedicalHistoryController(
            IPatientMedicalHistoryService medicalHistoryService)
        {
            _medicalHistoryService = medicalHistoryService;
        }

        // GET: api/Patients/{patientId}/medical-history
        [HttpGet("medical-history")]
        public async Task<IActionResult> GetMedicalHistory(Guid patientId)
        {
            var result = await _medicalHistoryService
                .GetMedicalHistoryAsync(patientId);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<MedicalHistoryResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            return Ok(
                ApiResponse<MedicalHistoryResponseDto>.SuccessResponse(
                    result,
                    "Medical history retrieved successfully."
                )
            );
        }

        // POST: api/Patients/{patientId}/medical-history
        // Admin: any patient in the hospital.
        // Doctor: only patients assigned to them.
        [HttpPost("medical-history")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> AddMedicalHistoryEntry(
            Guid patientId,
            [FromBody] CreateMedicalHistoryEntryDto dto)
        {
            var (status, entry) = await _medicalHistoryService
                .CreateEntryAsync(patientId, dto);

            if (status == CreateMedicalHistoryStatus.PatientNotFound)
            {
                return NotFound(
                    ApiResponse<MedicalHistoryEntryResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            if (status == CreateMedicalHistoryStatus.PatientInactive)
            {
                return Conflict(
                    ApiResponse<MedicalHistoryEntryResponseDto>.FailureResponse(
                        "PATIENT_INACTIVE",
                        "Cannot add medical history to an inactive patient."
                    )
                );
            }

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<MedicalHistoryEntryResponseDto>.SuccessResponse(
                    entry!,
                    "Medical history entry added successfully."
                )
            );
        }
    }
}
