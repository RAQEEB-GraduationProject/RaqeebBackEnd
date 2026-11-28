using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.Common;
using RAQEEB.DTOs.Users;
using RAQEEB.Entities;

namespace RAQEEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUser _currentUser;
        private readonly ApplicationDbContext _context;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            ICurrentUser currentUser,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _currentUser = currentUser;
            _context = context;
        }

        // GET: api/Users
        // GET: api/Users?role=Doctor
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? role = null)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var users = _context.Users
                .AsNoTracking()
                .Where(u => u.HospitalId == hospitalId.Value);

            // Optional role filter (case-insensitive, like before).
            if (!string.IsNullOrWhiteSpace(role))
            {
                var normalizedRole = role.Trim().ToUpperInvariant();

                users = users.Where(u =>
                    _context.UserRoles.Any(ur =>
                        ur.UserId == u.Id &&
                        _context.Roles.Any(r =>
                            r.Id == ur.RoleId &&
                            r.NormalizedName == normalizedRole)));
            }

            // One SQL query for all users and their role
            // (before: one extra query per user via GetRolesAsync).
            var result = await users
                .OrderBy(u => u.UserName)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    FullName = u.FullName == null || u.FullName.Trim() == "" ? null : u.FullName,
                    Email = u.Email == null || u.Email.Trim() == "" ? null : u.Email,
                    PhoneNumber = u.PhoneNumber == null || u.PhoneNumber.Trim() == "" ? null : u.PhoneNumber,
                    Role = (
                        from ur in _context.UserRoles
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where ur.UserId == u.Id
                        select r.Name
                    ).FirstOrDefault() ?? string.Empty,
                    HospitalId = u.HospitalId,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(
                ApiResponse<List<UserResponseDto>>.SuccessResponse(
                    result,
                    result.Count == 0
                        ? "No users found."
                        : "Users retrieved successfully."
                )
            );
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.HospitalId != hospitalId.Value)
            {
                return NotFound(
                    ApiResponse<UserResponseDto>.FailureResponse(
                        "USER_NOT_FOUND",
                        "User not found in your hospital."
                    )
                );
            }

            var result = await ToResponseAsync(user);

            return Ok(
                ApiResponse<UserResponseDto>.SuccessResponse(
                    result,
                    "User retrieved successfully."
                )
            );
        }

        // Edit account details (full name, email, phone number) of a user in the
        // Admin's hospital. UserName, role and password are not changed here.
        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(
            string id,
            [FromBody] UpdateUserDto dto)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.HospitalId != hospitalId.Value)
            {
                return NotFound(
                    ApiResponse<UserResponseDto>.FailureResponse(
                        "USER_NOT_FOUND",
                        "User not found in your hospital."
                    )
                );
            }

            var email = dto.Email.Trim();

            // The email must not belong to another account (any hospital).
            var emailOwner = await _userManager.FindByEmailAsync(email);

            if (emailOwner != null && emailOwner.Id != user.Id)
            {
                return Conflict(
                    ApiResponse<UserResponseDto>.FailureResponse(
                        "EMAIL_TAKEN",
                        "This email is already registered.",
                        new List<ApiError>
                        {
                            new() { Field = "Email", Message = "This email is already registered." }
                        }
                    )
                );
            }

            user.FullName = string.IsNullOrWhiteSpace(dto.FullName) ? null : dto.FullName.Trim();
            user.Email = email;
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            // UpdateAsync validates the user and also refreshes NormalizedEmail,
            // which Identity uses for email lookups.
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(
                    ApiResponse<UserResponseDto>.FailureResponse(
                        "VALIDATION_ERROR",
                        "One or more validation errors occurred.",
                        result.Errors
                            .Select(e => new ApiError
                            {
                                Field = e.Code.Contains("Email") ? "Email" : string.Empty,
                                Message = e.Description
                            })
                            .ToList()
                    )
                );
            }

            return Ok(
                ApiResponse<UserResponseDto>.SuccessResponse(
                    await ToResponseAsync(user),
                    "User updated successfully."
                )
            );
        }

        // Builds the user response (same shape as GET /api/Users).
        private async Task<UserResponseDto> ToResponseAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserResponseDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? null : user.FullName,
                Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
                PhoneNumber = string.IsNullOrWhiteSpace(user.PhoneNumber) ? null : user.PhoneNumber,
                Role = roles.FirstOrDefault() ?? string.Empty,
                HospitalId = user.HospitalId,
                IsActive = user.IsActive
            };
        }

        // Deactivate a login account (soft delete). The user is blocked at once,
        // even if they still have a valid token (see OnTokenValidated in Program.cs).
        // PATCH: api/Users/{id}/deactivate
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            return await SetUserActiveAsync(id, isActive: false);
        }

        // Reactivate a login account. Also needed for a removed patient:
        // PATCH api/Patients/{id}/reactivate brings back only the clinical
        // profile, and this endpoint lets the patient log in again.
        // PATCH: api/Users/{id}/activate
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            return await SetUserActiveAsync(id, isActive: true);
        }

        private async Task<IActionResult> SetUserActiveAsync(string id, bool isActive)
        {
            var hospitalId = _currentUser.HospitalId;

            if (hospitalId == null)
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null || user.HospitalId != hospitalId.Value)
            {
                return NotFound(
                    ApiResponse<UserResponseDto>.FailureResponse(
                        "USER_NOT_FOUND",
                        "User not found in your hospital."
                    )
                );
            }

            // An admin must not lock themselves out by mistake.
            if (!isActive && user.Id == _currentUser.UserId)
            {
                return BadRequest(
                    ApiResponse<UserResponseDto>.FailureResponse(
                        "CANNOT_DEACTIVATE_SELF",
                        "You cannot deactivate your own account."
                    )
                );
            }

            if (user.IsActive == isActive)
            {
                return Conflict(
                    ApiResponse<UserResponseDto>.FailureResponse(
                        isActive ? "USER_ALREADY_ACTIVE" : "USER_ALREADY_INACTIVE",
                        isActive
                            ? "This user is already active."
                            : "This user is already inactive."
                    )
                );
            }

            user.IsActive = isActive;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<UserResponseDto>.FailureResponse(
                        "USER_UPDATE_FAILED",
                        "The user could not be updated.",
                        result.Errors
                            .Select(e => new ApiError { Message = e.Description })
                            .ToList()
                    )
                );
            }

            return NoContent();
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccount()
        {
            // Get the ID of the currently authenticated user.
            var userId = _currentUser.UserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            // Find that user in the database.
            var user = await _userManager.FindByIdAsync(userId);

            // Make sure the account exists and is active.
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            // Get the user's roles.
            var roles = await _userManager.GetRolesAsync(user);

            // Return the account information.
            var result = new
            {
                user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.HospitalId,
                user.IsActive,
                user.CreatedAt,
                Roles = roles
            };

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    result,
                    "Account information retrieved successfully."
                )
            );
        }

    }
}