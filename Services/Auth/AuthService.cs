using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAQEEB.Data;
using RAQEEB.DTOs.Auth;
using RAQEEB.Entities;

namespace RAQEEB.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
        }

        public async Task<LoginResponseDto?> LoginAsync(
            LoginDto loginDto)
        {
            var user = await _userManager
                .FindByNameAsync(loginDto.UserName);

            if (user == null)
            {
                return null;
            }

            if (!user.IsActive)
            {
                return null;
            }

            var passwordValid =
                await _userManager.CheckPasswordAsync(
                    user,
                    loginDto.Password);

            if (!passwordValid)
            {
                return null;
            }

            var roles =
                await _userManager.GetRolesAsync(user);

            Guid? patientId = null;

            if (roles.Contains("Patient"))
            {
                patientId = await _context.Patients
                    .Where(p => p.UserId == user.Id)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefaultAsync();
            }


            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id),

                new Claim(
                    ClaimTypes.Name,
                    user.UserName!),

                new Claim(
                    "HospitalId",
                    user.HospitalId.ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role));
            }

            var jwtKey = _configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT key is not configured.");
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expiration =
                DateTime.UtcNow.AddHours(2);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: credentials);

            var tokenString =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return new LoginResponseDto
            {
                Token = tokenString,
                Expiration = expiration,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email! ?? String.Empty,
                PhoneNumber = user.PhoneNumber,
                HospitalId = user.HospitalId,
                Roles = roles.ToList(),
                PatientId = patientId

            };
        }

        public async Task<RegisterResponseDto?> RegisterAsync(
                    RegisterDto registerDto,
                    Guid hospitalId)
        {
            // 1. Check whether the username already exists.
            var existingUser = await _userManager
                .FindByNameAsync(registerDto.UserName);

            if (existingUser != null)
            {
                return null;
            }

            // 2. Check whether the email already exists.
            var existingEmail = await _userManager
                .FindByEmailAsync(registerDto.Email);

            if (existingEmail != null)
            {
                return null;
            }

            // 3. Validate the selected role.
            var allowedRoles = new[]
            {
        "Doctor",
        "Nurse",
        "Caregiver",
        "Patient"
    };

            if (!allowedRoles.Contains(registerDto.Role))
            {
                return null;
            }

            // 4. Begin a database transaction.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 5. Create the Identity account.
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                HospitalId = hospitalId,
                IsActive = true,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(registerDto.FullName)
                    ? null
                    : registerDto.FullName.Trim(),
            };

            var result = await _userManager.CreateAsync(
                user,
                registerDto.Password);

            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return null;
            }

            // 6. Assign the selected role.
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                registerDto.Role);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return null;
            }

            // 7. Commit the transaction.
            await transaction.CommitAsync();

            // 8. Return the newly created account information.
            return new RegisterResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = registerDto.Role
            };
        }
    }
}