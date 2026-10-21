using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Common;
using RAQEEB.DTOs.Users;
using RAQEEB.Entities;

namespace RAQEEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUser _currentUser;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            ICurrentUser currentUser)
        {
            _userManager = userManager;
            _currentUser = currentUser;
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

            var users = _userManager.Users
                .Where(u => u.HospitalId == hospitalId.Value);

            var userList = await users.ToListAsync();

            var result = new List<UserResponseDto>();

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (!string.IsNullOrWhiteSpace(role) &&
                    !roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(new UserResponseDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? string.Empty,
                    HospitalId = user.HospitalId,
                    IsActive = user.IsActive
                });
            }

            return Ok(result);
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
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var result = new UserResponseDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                HospitalId = user.HospitalId,
                IsActive = user.IsActive
            };

            return Ok(result);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyAccount()
        {
            // Get the ID of the currently authenticated user.
            var userId = _currentUser.UserId;

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
            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.HospitalId,
                user.IsActive,
                user.CreatedAt,
                Roles = roles
            });
        }

    }
}