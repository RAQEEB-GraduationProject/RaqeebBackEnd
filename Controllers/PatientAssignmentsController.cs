using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.PatientAssignments;
using RAQEEB.Entities;

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

        public PatientAssignmentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrentUser currentUser)
        {
            _context = context;
            _userManager = userManager;
            _currentUser = currentUser;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAssignment(
    [FromBody] CreatePatientAssignmentDto dto)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            // 1. Find the patient in the Admin's hospital.
            var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                p.Id == dto.PatientId &&
                p.HospitalId == hospitalId.Value &&
                p.IsActive);

            if (patient == null)
            {
                return NotFound(new
                {
                    message = "Active patient not found in your hospital."
                });
            }

            // 2. Find the user being assigned.
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user == null ||
                user.HospitalId != hospitalId.Value ||
                !user.IsActive)
            {
                return BadRequest(new
                {
                    message = "The specified user is invalid or does not belong to your hospital."
                });
            }

            // 3. Check that the user has an allowed role.
            var allowedRoles = new[] { "Doctor", "Nurse", "Caregiver" };

            var userRoles = await _userManager.GetRolesAsync(user);

            if (!userRoles.Any(role => allowedRoles.Contains(role)))
            {
                return BadRequest(new
                {
                    message = "Only Doctors, Nurses, and Caregivers can be assigned to patients."
                });
            }

            // 4. Check for an existing active assignment.
            var existingAssignment = await _context.PatientAssignments
                .AnyAsync(a =>
                    a.PatientId == patient.Id &&
                    a.UserId == user.Id &&
                    a.IsActive);

            if (existingAssignment)
            {
                return Conflict(new
                {
                    message = "This user is already assigned to this patient."
                });
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
            return StatusCode(201, new
            {
                assignment.Id,
                assignment.PatientId,
                assignment.UserId,
                assignment.AssignedAt,
                assignment.IsActive
            });


        }

        [HttpGet]
        public async Task<IActionResult> GetAssignments()
        {
            var hospitalId = _currentUser.HospitalId;
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (hospitalId == null || userId == null)
                return Forbid();

            var query = _context.PatientAssignments
                .AsNoTracking()
                .Where(a => a.Patient.HospitalId == hospitalId.Value);

            if (role != "Admin")
            {
                query = query.Where(a => a.UserId == userId); //doctors see only their patients
            }

            var assignments = await query
                .Select(a => new
                {
                    a.Id,
                    a.PatientId,
                    PatientUserName = a.Patient.User.UserName ?? string.Empty,
                    a.UserId,
                    AssignedUserName = a.User.UserName ?? string.Empty,
                    a.AssignedAt,
                    a.IsActive
                })
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetAssignmentById(Guid id)
        {
            var hospitalId = _currentUser.HospitalId;
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (hospitalId == null || userId == null)
                return Forbid();

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
                .Select(a => new
                {
                    a.Id,
                    a.PatientId,
                    PatientUserName = a.Patient.User.UserName ?? string.Empty,
                    a.UserId,
                    AssignedUserName = a.User.UserName ?? string.Empty,
                    a.AssignedAt,
                    a.IsActive
                })
                .FirstOrDefaultAsync();

            if (assignment == null)
                return NotFound();

            return Ok(assignment);
        }
    }
}