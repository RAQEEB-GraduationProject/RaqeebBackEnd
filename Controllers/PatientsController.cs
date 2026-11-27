using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.DTOs.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.Patients;
using RAQEEB.Entities;
using RAQEEB.Services.Patients;

namespace RAQEEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUser _currentUser;
        private readonly IPatientAccessService _patientAccess;

        public PatientsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrentUser currentUser,
            IPatientAccessService patientAccess)
        {
            _context = context;
            _userManager = userManager;
            _currentUser = currentUser;
            _patientAccess = patientAccess;
        }

        // POST: api/Patients
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePatient(
            [FromBody] CreatePatientDto dto)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null || user.HospitalId != hospitalId.Value)
            {
                return BadRequest(
                ApiResponse<PatientResponseDto>.FailureResponse(
                    "INVALID_USER",
                    "The specified user does not belong to your hospital."
                )
            );
            } //verify that user belongs to same hospital as admin or doctor

            if (!await _userManager.IsInRoleAsync(user, "Patient"))
            {
                return BadRequest(
                ApiResponse<PatientResponseDto>.FailureResponse(
                    "INVALID_ROLE",
                    "The specified user must have the Patient role."
                )
            );
            }

            var profileExists = await _context.Patients
                .AnyAsync(p => p.UserId == dto.UserId); //does any patient matches the id ? boolean

            if (profileExists)
            {
                return Conflict(
                ApiResponse<PatientResponseDto>.FailureResponse(
                    "PATIENT_PROFILE_EXISTS",
                    "This user already has a patient profile."
                )
            );
            }

            // Trim once and use the same value for the check and for saving
            // (before: " MRN-1" passed the check, then crashed on the unique index).
            var medicalRecordNumber = string.IsNullOrWhiteSpace(dto.MedicalRecordNumber)
                ? null
                : dto.MedicalRecordNumber.Trim();

            if (medicalRecordNumber != null)
            {
                var recordExists = await _context.Patients
                    .AnyAsync(p =>
                        p.HospitalId == hospitalId.Value && //The existing patient belongs to the same hospital.
                        p.MedicalRecordNumber == medicalRecordNumber);//The existing patient's medical record number
                                                                          //matches the submitted number


                //medical record numbers are being treated as unique within each hospital
                if (recordExists)
                {
                    return Conflict(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "MRN_ALREADY_EXISTS",
                        "This medical record number is already in use."
                    )
                );
                }
            }


            var patient = new Patient
            {
                UserId = user.Id,
                HospitalId = hospitalId.Value,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                BloodGroup = dto.BloodGroup?.Trim(),
                MedicalRecordNumber = medicalRecordNumber,
                Diagnosis = dto.Diagnosis,
                Stage = dto.Stage,
                Treatment = dto.Treatment,
                Protocol = dto.Protocol,
                RiskLevel = dto.RiskLevel,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);

            await _context.SaveChangesAsync();

            // Read the new profile back with the shared mapping (same shape as GET).
            var result = await GetPatientResponseAsync(patient.Id);

            return StatusCode(
                StatusCodes.Status201Created,
                ApiResponse<PatientResponseDto>.SuccessResponse(
                    result,
                    "Patient created successfully."
                )
            );
        }

        // GET: api/Patients
        // Who sees what is decided in one place: IPatientAccessService.
        [HttpGet]
        public async Task<IActionResult> GetPatients()
        {
            if (_currentUser.HospitalId == null || _currentUser.UserId == null)
                return Forbid();

            var patients = await _patientAccess.GetAccessiblePatients()
                .Select(PatientResponseDto.Projection)
                .ToListAsync();

            return Ok(
                ApiResponse<List<PatientResponseDto>>.SuccessResponse(
                    patients,
                    patients.Count == 0
                        ? "No patients found."
                        : "Patients retrieved successfully."
                )
            );
        }



        // GET: api/Patients/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPatientById(Guid id)
        {
            if (_currentUser.HospitalId == null || _currentUser.UserId == null)
                return Forbid();

            var patient = await _patientAccess.GetAccessiblePatients()
                .Where(p => p.Id == id)
                .Select(PatientResponseDto.Projection)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return NotFound(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or you do not have access to this patient."
                    )
                );
            }

            return Ok(
                ApiResponse<PatientResponseDto>.SuccessResponse(
                    patient,
                    "Patient retrieved successfully."
                )
            );
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePatient(
    Guid id,
    [FromBody] UpdatePatientDto dto)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.HospitalId == hospitalId.Value &&
                p.IsActive);

            if (patient == null)
            {
                return NotFound(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found in your hospital."
                    )
                );
            }

            if (!string.IsNullOrWhiteSpace(dto.MedicalRecordNumber))
            {
                var recordNumber = dto.MedicalRecordNumber.Trim();

                var recordExists = await _context.Patients.AnyAsync(p =>
                    p.HospitalId == hospitalId.Value &&
                    p.Id != id &&
                    p.MedicalRecordNumber == recordNumber);

                if (recordExists)
                {

                    return Conflict(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "MRN_ALREADY_EXISTS",
                        "This medical record number is already in use."
                    )
                );
                }

                patient.MedicalRecordNumber = recordNumber;
            }
            else
            {
                patient.MedicalRecordNumber = null;
            }

            patient.DateOfBirth = dto.DateOfBirth;
            patient.Gender = dto.Gender;
            patient.BloodGroup = dto.BloodGroup?.Trim();
            patient.Diagnosis = dto.Diagnosis;
            patient.Stage = dto.Stage;
            patient.Treatment = dto.Treatment;
            patient.Protocol = dto.Protocol;
            patient.RiskLevel = dto.RiskLevel;

            await _context.SaveChangesAsync();

            var result = await GetPatientResponseAsync(id);

            return Ok(
                ApiResponse<PatientResponseDto>.SuccessResponse(
                    result,
                    "Patient updated successfully."
                )
            );
        }


        // Delete a patient profile (soft delete)
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePatient(Guid id)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.HospitalId == hospitalId.Value &&
                p.IsActive);

            if (patient == null)
            {
                return NotFound(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found or already inactive."
                    )
                );
            }

            // Find the Identity account associated with this patient.
            var user = await _userManager.FindByIdAsync(patient.UserId);

            if (user == null)
            {
                return Conflict(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "PATIENT_USER_NOT_FOUND",
                        "The patient profile has no associated user account."
                    )
                );
            }

            // Start a transaction to update both records together.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Deactivate the clinical patient profile.
            patient.IsActive = false;

            // Deactivate the associated login account.
            user.IsActive = false;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return NoContent();
        }


        // Reactivate a patient profile
        [HttpPatch("{id:guid}/reactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivatePatient(Guid id)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.HospitalId == hospitalId.Value);

            if (patient == null)
            {
                return NotFound(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Patient not found in your hospital."
                    )
                );
            }

            if (patient.IsActive)
            {
                return Conflict(
                    ApiResponse<PatientResponseDto>.FailureResponse(
                        "PATIENT_ALREADY_ACTIVE",
                        "This patient is already active."
                    )
                );
            }

            // Reactivate only the clinical patient profile.
            patient.IsActive = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Loads one patient with the shared mapping (used after create / update).
        private Task<PatientResponseDto> GetPatientResponseAsync(Guid id)
        {
            return _context.Patients
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(PatientResponseDto.Projection)
                .FirstAsync();
        }
    }
}
