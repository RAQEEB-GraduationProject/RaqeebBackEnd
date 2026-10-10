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
    [Authorize(Roles = "Admin")]
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
    }
}