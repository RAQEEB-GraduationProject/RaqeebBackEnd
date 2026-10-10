using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.Patients;
using RAQEEB.Entities;

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

        public PatientsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICurrentUser currentUser)
        {
            _context = context;
            _userManager = userManager;
            _currentUser = currentUser;
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
                return BadRequest(new
                {
                    message = "The specified user does not belong to your hospital."
                });
            } //verify that user belongs to same hospital as admin or doctor 

            if (!await _userManager.IsInRoleAsync(user, "Patient"))
            {
                return BadRequest(new
                {
                    message = "The specified user must have the Patient role."
                });
            }

            var profileExists = await _context.Patients
                .AnyAsync(p => p.UserId == dto.UserId); //does any patient matches the id ? boolean

            if (profileExists)
            {
                return Conflict(new  //409
                {
                    message = "This user already has a patient profile."
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.MedicalRecordNumber))
            {
                var recordExists = await _context.Patients
                    .AnyAsync(p =>
                        p.HospitalId == hospitalId.Value &&
                        p.MedicalRecordNumber == dto.MedicalRecordNumber);

                if (recordExists)
                {
                    return Conflict(new
                    {
                        message = "This medical record number is already in use."
                    });
                }
            }

            var patient = new Patient
            {
                UserId = user.Id,
                HospitalId = hospitalId.Value,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                MedicalRecordNumber = string.IsNullOrWhiteSpace(
                    dto.MedicalRecordNumber)
                    ? null
                    : dto.MedicalRecordNumber.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Patients.Add(patient);

            await _context.SaveChangesAsync();

            var result = new PatientResponseDto
            {
                Id = patient.Id,
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                HospitalId = patient.HospitalId,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                MedicalRecordNumber = patient.MedicalRecordNumber,
                IsActive = patient.IsActive,
                CreatedAt = patient.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetPatientById),
                new { id = patient.Id },
                result);
        }

        // GET: api/Patients
        [HttpGet]
        public async Task<IActionResult> GetPatients()
        {
            var hospitalId = _currentUser.HospitalId;
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (hospitalId == null || userId == null)
                return Forbid();

            var query = _context.Patients
                .AsNoTracking()
                .Where(p =>
                    p.HospitalId == hospitalId.Value &&
                    p.IsActive);

            if (role != "Admin")
            {
                if (role == "Patient")
                {
                    query = query.Where(p => p.UserId == userId);
                }
                else
                {
                    query = query.Where(p =>
                        _context.PatientAssignments.Any(a =>
                            a.PatientId == p.Id &&
                            a.UserId == userId &&
                            a.IsActive));
                }
            }

            var patients = await query
                .Select(p => new PatientResponseDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserName = p.User.UserName ?? string.Empty,
                    Email = p.User.Email ?? string.Empty,
                    HospitalId = p.HospitalId,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    MedicalRecordNumber = p.MedicalRecordNumber,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(patients);
        }

        // GET: api/Patients/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPatientById(Guid id)
        {
            var hospitalId = _currentUser.HospitalId;
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (hospitalId == null || userId == null)
                return Forbid();

            var query = _context.Patients
                .AsNoTracking()
                .Where(p =>
                    p.Id == id &&
                    p.HospitalId == hospitalId.Value &&
                    p.IsActive);

            if (role != "Admin")
            {
                if (role == "Patient")
                {
                    query = query.Where(p => p.UserId == userId);
                }
                else
                {
                    query = query.Where(p =>
                        _context.PatientAssignments.Any(a =>
                            a.PatientId == p.Id &&
                            a.UserId == userId &&
                            a.IsActive));
                }
            }

            var patient = await query
                .Select(p => new PatientResponseDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserName = p.User.UserName ?? string.Empty,
                    Email = p.User.Email ?? string.Empty,
                    HospitalId = p.HospitalId,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    MedicalRecordNumber = p.MedicalRecordNumber,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (patient == null)
                return NotFound();

            return Ok(patient);
        }
    }
}