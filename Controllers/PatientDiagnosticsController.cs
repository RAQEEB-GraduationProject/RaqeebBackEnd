using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RAQEEB.DTOs.Common;
using RAQEEB.DTOs.Diagnostics;
using RAQEEB.Services.Patients;

namespace RAQEEB.Controllers
{
    // Read-only diagnostics (labs, imaging and their PDF reports).
    // Access: Admin (own hospital), Doctor / Nurse / Caregiver (active assignment),
    // Patient (own profile) - enforced by IPatientAccessService.
    [ApiController]
    [Route("api/Patients/{patientId:guid}/diagnostics")]
    [Authorize]
    public class PatientDiagnosticsController : ControllerBase
    {
        private readonly IPatientDiagnosticsService _diagnosticsService;

        public PatientDiagnosticsController(
            IPatientDiagnosticsService diagnosticsService)
        {
            _diagnosticsService = diagnosticsService;
        }

        // GET: api/Patients/{patientId}/diagnostics/labs
        [HttpGet("labs")]
        [ProducesResponseType(typeof(ApiResponse<List<LabResultResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<LabResultResponseDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetLabResults(Guid patientId)
        {
            var result = await _diagnosticsService
                .GetLabResultsAsync(patientId);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<List<LabResultResponseDto>>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            return Ok(
                ApiResponse<List<LabResultResponseDto>>.SuccessResponse(
                    result,
                    "Lab results retrieved successfully."
                )
            );
        }

        // GET: api/Patients/{patientId}/diagnostics/imaging
        [HttpGet("imaging")]
        [ProducesResponseType(typeof(ApiResponse<List<ImagingResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<List<ImagingResponseDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetImaging(Guid patientId)
        {
            var result = await _diagnosticsService
                .GetImagingAsync(patientId);

            if (result == null)
            {
                return NotFound(
                    ApiResponse<List<ImagingResponseDto>>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            return Ok(
                ApiResponse<List<ImagingResponseDto>>.SuccessResponse(
                    result,
                    "Imaging records retrieved successfully."
                )
            );
        }

        // GET: api/Patients/{patientId}/diagnostics/files/{fileId}
        // Returns the PDF inline (view in browser); add ?download=true to download it.
        [HttpGet("files/{fileId:guid}")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK, "application/pdf")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFile(
            Guid patientId,
            Guid fileId,
            [FromQuery] bool download = false)
        {
            var (status, file) = await _diagnosticsService
                .GetFileAsync(patientId, fileId);

            if (status == DiagnosticFileStatus.PatientNotFound)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            if (status == DiagnosticFileStatus.FileNotFound)
            {
                return NotFound(
                    ApiResponse<object>.FailureResponse(
                        "FILE_NOT_FOUND",
                        "Diagnostic file not found."
                    )
                );
            }

            // Private medical document: never cache, never sniff.
            Response.Headers.CacheControl = "no-store, no-cache";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.XContentTypeOptions = "nosniff";

            var disposition = new ContentDispositionHeaderValue(
                download ? "attachment" : "inline");
            disposition.SetHttpFileName(file!.FileName);
            Response.Headers.ContentDisposition = disposition.ToString();

            return PhysicalFile(
                file.FullPath,
                file.ContentType,
                enableRangeProcessing: true);
        }
    }
}
