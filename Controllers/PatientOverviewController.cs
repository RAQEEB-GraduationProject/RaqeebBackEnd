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
    public class PatientOverviewController : ControllerBase
    {
        private readonly IPatientOverviewService _overviewService;

        public PatientOverviewController(
            IPatientOverviewService overviewService)
        {
            _overviewService = overviewService;
        }

        // GET: api/Patients/{patientId}/overview
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(Guid patientId)
        {
            var result = await _overviewService.GetOverviewAsync(patientId);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<PatientOverviewResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            return Ok(
                ApiResponse<PatientOverviewResponseDto>.SuccessResponse(
                    result,
                    "Patient overview retrieved successfully."
                )
            );
        }
    }
}
