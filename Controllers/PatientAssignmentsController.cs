using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.Common;
using RAQEEB.DTOs.PatientAssignments;
using RAQEEB.Entities;
using RAQEEB.Services.Patients;

namespace RAQEEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PatientAssignmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUser _currentUser;
        private readonly IPatientAccessService _patientAccess;

        public PatientAssignmentsController(
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAssignment(
    [FromBody] CreatePatientAssignmentDto dto)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "FORBIDDEN",
                        "You are not authorized to access this resource."
                    )
                );
            }

            // 1. Find the patient in the Admin's hospital.
            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.Id == dto.PatientId &&
                p.HospitalId == hospitalId.Value &&
                p.IsActive);

            if (patient == null)
            {
                return NotFound(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "PATIENT_NOT_FOUND",
                        "Active patient not found in your hospital."
                    )
                );
            }

            // 2. Find the user being assigned.
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null ||
                user.HospitalId != hospitalId.Value ||
                !user.IsActive)
            {
                return BadRequest(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "INVALID_USER",
                        "The specified user is invalid or does not belong to your hospital."
                    )
                );
            }

            // 3. Check that the user has an allowed role.
            var allowedRoles = new[] { "Doctor", "Nurse", "Caregiver" };

            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Any(role => allowedRoles.Contains(role)))
            {
                return BadRequest(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "INVALID_ASSIGNMENT_ROLE",
                        "Only Doctors, Nurses, and Caregivers can be assigned to patients."
                    )
                );
            }

            // 4. Check for an existing active assignment.
            var existingAssignment = await _context.PatientAssignments
                .AnyAsync(a =>
                    a.PatientId == patient.Id &&
                    a.UserId == user.Id &&
                    a.IsActive);

            if (existingAssignment)
            {
                return Conflict(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "ASSIGNMENT_ALREADY_EXISTS",
                        "This user is already assigned to this patient."
                    )
                );
            }

            // 5. Create the assignment.
            var assignment = new PatientAssignment
            {
                PatientId = patient.Id,
                UserId = user.Id,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.PatientAssignments.Add(assignment);

            await _context.SaveChangesAsync();

            // 6. Return the created assignment.
            // New API Response structure
            var response = new PatientAssignmentResponseDto
            {
                Id = assignment.Id,
                PatientId = assignment.PatientId,
                UserId = assignment.UserId,
                AssignedAt = assignment.AssignedAt,
                IsActive = assignment.IsActive
            };

            return StatusCode(
                201,
                ApiResponse<PatientAssignmentResponseDto>.SuccessResponse(
                    response,
                    "Patient assignment created successfully"
                )
            );


        }

        // GET: api/PatientAssignments/Assignments
        [HttpGet("Assignments")]
        public async Task<IActionResult> GetPatientsOverview()
        {
            var hospitalId = _currentUser.HospitalId;
            var userId = _currentUser.UserId;

            if (hospitalId == null || userId == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ApiResponse<List<PatientStaffOverviewDto>>.FailureResponse(
                        "FORBIDDEN",
                        "You are not authorized to access this resource."
                    )
                );
            }

            // Active patients the current user may see (rules live in IPatientAccessService).
            var query = _patientAccess.GetAccessiblePatients()
                .Where(p => p.IsActive);

            // Get the basic patient information
            var patients = await query
                .Select(p => new PatientStaffOverviewDto
                {
                    PatientId = p.Id,
                    PatientName = p.User.FullName ?? p.User.UserName ?? string.Empty,
                    PatientCode = p.MedicalRecordNumber
                })
                .ToListAsync();

            // Return an empty list if no patients are found
            if (patients.Count == 0)
            {
                return Ok(
                    ApiResponse<List<PatientStaffOverviewDto>>.SuccessResponse(
                        patients,
                        "No patients found"
                    )
                );
            }

            var patientIds = patients
                .Select(p => p.PatientId)
                .ToList();

            // Get the active staff assignments and their roles
            var assignments = await (
                from a in _context.PatientAssignments.AsNoTracking()
                join u in _context.Users.AsNoTracking()
                    on a.UserId equals u.Id
                join ur in _context.UserRoles.AsNoTracking()
                    on u.Id equals ur.UserId
                join r in _context.Roles.AsNoTracking()
                    on ur.RoleId equals r.Id
                where patientIds.Contains(a.PatientId)
                    && a.IsActive
                    && u.IsActive
                    && u.HospitalId == hospitalId.Value
                    && (
                        r.Name == "Doctor" ||
                        r.Name == "Nurse" ||
                        r.Name == "Caregiver"
                    )
                select new
                {
                    AssignmentId = a.Id,
                    a.PatientId,
                    UserId = u.Id,
                    Name = u.FullName ?? u.UserName ?? string.Empty,
                    RoleName = r.Name
                }
            ).ToListAsync();

            // Match each staff member with their patient
            var patientsById = patients.ToDictionary(
                p => p.PatientId
            );

            foreach (var assignment in assignments)
            {
                if (!patientsById.TryGetValue(
                    assignment.PatientId,
                    out var patient))
                {
                    continue;
                }

                var staff = new AssignedStaffDto
                {
                    AssignmentId = assignment.AssignmentId,
                    UserId = assignment.UserId,
                    Name = assignment.Name
                };

                switch (assignment.RoleName)
                {
                    case "Doctor":
                        patient.AssignedDoctor.Add(staff);
                        break;

                    case "Nurse":
                        patient.AssignedNurses.Add(staff);
                        break;

                    case "Caregiver":
                        patient.AssignedCaregivers.Add(staff);
                        break;
                }
            }

            return Ok(
                ApiResponse<List<PatientStaffOverviewDto>>.SuccessResponse(
                patients,
                "Patients retrieved successfully"
                )
            );
        }


        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAssignmentById(Guid id)
        {
            var hospitalId = _currentUser.HospitalId;
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (hospitalId == null || userId == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ApiResponse<PatientAssignmentDetailsDto>.FailureResponse(
                        "FORBIDDEN",
                        "You are not authorized to access this resource."
                    )
                );
            }

            var query = _context.PatientAssignments
                .AsNoTracking()
                .Where(a =>
                    a.Id == id &&
                    a.Patient.HospitalId == hospitalId.Value);

            if (role != "Admin")
            {
                query = query.Where(a => a.UserId == userId);
            }

            var assignment = await query
                .Select(a => new PatientAssignmentDetailsDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientUserName = a.Patient.User.UserName ?? string.Empty,
                    PatientName = a.Patient.User.FullName ?? a.Patient.User.UserName ?? string.Empty,
                    UserId = a.UserId,
                    AssignedUserName = a.User.UserName ?? string.Empty,
                    AssignedName = a.User.FullName ?? a.User.UserName ?? string.Empty,
                    AssignedAt = a.AssignedAt,
                    IsActive = a.IsActive
                })
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return NotFound(
                    ApiResponse<PatientAssignmentDetailsDto>.FailureResponse(
                        "ASSIGNMENT_NOT_FOUND",
                        "Assignment not found or you do not have access to it."
                    )
                );
            }

            return Ok(
                ApiResponse<PatientAssignmentDetailsDto>.SuccessResponse(
                    assignment,
                    "Assignment retrieved successfully"
                )
            );
        }



        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignUser(Guid id)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "FORBIDDEN",
                        "You are not authorized to access this resource."
                    )
                );
            }

            // 1. Find the assignment in the Admin's hospital.
            var assignment = await _context.PatientAssignments
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Patient.HospitalId == hospitalId.Value);

            if (assignment == null)
            {
                return NotFound(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "ASSIGNMENT_NOT_FOUND",
                        "Assignment not found in your hospital."
                    )
                );
            }

            // 2. Check whether the assignment is already inactive.
            if (!assignment.IsActive)
            {
                return Conflict(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "ASSIGNMENT_ALREADY_INACTIVE",
                        "This assignment has already been deactivated."
                    )
                );
            }

            // 3. Soft delete: deactivate the assignment.
            assignment.IsActive = false;

            await _context.SaveChangesAsync();

            // 4. Return the updated assignment.
            var response = new PatientAssignmentResponseDto
            {
                Id = assignment.Id,
                PatientId = assignment.PatientId,
                UserId = assignment.UserId,
                AssignedAt = assignment.AssignedAt,
                IsActive = assignment.IsActive
            };

            return Ok(
                ApiResponse<PatientAssignmentResponseDto>.SuccessResponse(
                    response,
                    "User successfully unassigned from patient."
                )
            );
        }

        // Reactivate a previously deactivated assignment
        [HttpPatch("{id:guid}/reactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivateAssignment(Guid id)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "FORBIDDEN",
                        "You are not authorized to access this resource."
                    )
                );
            }

            // 1. Find the assignment in the Admin's hospital.
            var assignment = await _context.PatientAssignments
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    a.Patient.HospitalId == hospitalId.Value);

            if (assignment == null)
            {
                return NotFound(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "ASSIGNMENT_NOT_FOUND",
                        "Assignment not found in your hospital."
                    )
                );
            }

            // 2. Check whether the assignment is already active.
            if (assignment.IsActive)
            {
                return Conflict(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "ASSIGNMENT_ALREADY_ACTIVE",
                        "This assignment is already active."
                    )
                );
            }

            // 3. Prevent duplicate active assignments.
            var existingActiveAssignment = await _context.PatientAssignments
                .AnyAsync(a =>
                    a.Id != assignment.Id &&
                    a.PatientId == assignment.PatientId &&
                    a.UserId == assignment.UserId &&
                    a.IsActive);

            if (existingActiveAssignment)
            {
                return Conflict(
                    ApiResponse<PatientAssignmentResponseDto>.FailureResponse(
                        "ACTIVE_ASSIGNMENT_EXISTS",
                        "This user already has an active assignment to this patient."
                    )
                );
            }

            // 4. Reactivate the assignment.
            assignment.IsActive = true;

            await _context.SaveChangesAsync();

            // 5. Return the updated assignment.
            var response = new PatientAssignmentResponseDto
            {
                Id = assignment.Id,
                PatientId = assignment.PatientId,
                UserId = assignment.UserId,
                AssignedAt = assignment.AssignedAt,
                IsActive = assignment.IsActive
            };

            return Ok(
                ApiResponse<PatientAssignmentResponseDto>.SuccessResponse(
                    response,
                    "Assignment successfully reactivated."
                )
            );
        }



    }
}