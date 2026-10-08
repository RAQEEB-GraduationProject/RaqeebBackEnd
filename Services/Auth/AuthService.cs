using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using RAQEEB.DTOs.Auth;
using RAQEEB.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RAQEEB.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
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
                Role = roles.FirstOrDefault() ?? string.Empty,
                HospitalId = user.HospitalId
            };
        }

        public async Task<bool> RegisterAsync(
            RegisterDto registerDto,
            Guid hospitalId)
        {
            var existingUser = await _userManager
                .FindByNameAsync(registerDto.UserName);

            if (existingUser != null)
            {
                return false;
            }

            var existingEmail = await _userManager
                .FindByEmailAsync(registerDto.Email);

            if (existingEmail != null)
            {
                return false;
            }

            var allowedRoles = new[]
            {
                "Doctor",
                "Nurse",
                "Caregiver",
                "Patient"
            };

            if (!allowedRoles.Contains(registerDto.Role))
            {
                return false;
            }

            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                HospitalId = hospitalId,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(
                user,
                registerDto.Password);

            if (!result.Succeeded)
            {
                return false;
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                registerDto.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return false;
            }

            return true;
        }
    }
}