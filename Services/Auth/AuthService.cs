using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAQEEB.Data;
using RAQEEB.DTOs.Auth;
using RAQEEB.DTOs.Common;
using RAQEEB.Entities;

namespace RAQEEB.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _context = context;
        }

        public async Task<LoginResult> LoginAsync(
            LoginDto loginDto)
        {
            var user = await _userManager
                .FindByNameAsync(loginDto.UserName);

            if (user == null)
            {
                return LoginResult.Failed(LoginStatus.InvalidCredentials);
            }

            if (!user.IsActive)
            {
                return LoginResult.Failed(LoginStatus.InvalidCredentials);
            }

            // Checks the password AND counts wrong attempts:
            // after 5 wrong passwords the account is locked for 5 minutes
            // (settings in Program.cs). A correct password resets the counter.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                loginDto.Password,
                lockoutOnFailure: true);

            if (signInResult.IsLockedOut)
            {
                return LoginResult.Failed(LoginStatus.LockedOut);
            }

            if (!signInResult.Succeeded)
            {
                return LoginResult.Failed(LoginStatus.InvalidCredentials);
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

            return LoginResult.Succeeded(new LoginResponseDto
            {
                Token = tokenString,
                Expiration = expiration,
                UserId = user.Id,
                UserName = user.UserName!,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                HospitalId = user.HospitalId,
                Roles = roles.ToList(),
                PatientId = patientId
            });
        }

        public async Task<RegisterResult> RegisterAsync(
            RegisterDto registerDto,
            Guid hospitalId)
        {
            // 1. Check whether the username already exists.
            var existingUser = await _userManager
                .FindByNameAsync(registerDto.UserName);

            if (existingUser != null)
            {
                return RegisterResult.Failed(
                    RegisterStatus.UserNameTaken,
                    new List<ApiError>
                    {
                        new() { Field = "UserName", Message = "This username is already taken." }
                    });
            }

            // 2. Check whether the email already exists.
            var existingEmail = await _userManager
                .FindByEmailAsync(registerDto.Email);

            if (existingEmail != null)
            {
                return RegisterResult.Failed(
                    RegisterStatus.EmailTaken,
                    new List<ApiError>
                    {
                        new() { Field = "Email", Message = "This email is already registered." }
                    });
            }

            // 3. Validate the selected role ("doctor" and "Doctor" both work).
            var role = AllowedRegisterRoles.FirstOrDefault(r =>
                string.Equals(r, registerDto.Role?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (role == null)
            {
                return RegisterResult.Failed(
                    RegisterStatus.InvalidRole,
                    new List<ApiError>
                    {
                        new()
                        {
                            Field = "Role",
                            Message = $"Role must be one of: {string.Join(", ", AllowedRegisterRoles)}."
                        }
                    });
            }

            // 4. Begin a database transaction.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 5. Create the Identity account.
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName.Trim(),
                Email = registerDto.Email.Trim(),
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

                // e.g. "Passwords must have at least one digit ('0'-'9')."
                return RegisterResult.Failed(
                    RegisterStatus.InvalidData,
                    ToApiErrors(result));
            }

            // 6. Assign the selected role.
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                role);

            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return RegisterResult.Failed(
                    RegisterStatus.RoleAssignmentFailed,
                    ToApiErrors(roleResult));
            }

            // 7. Commit the transaction.
            await transaction.CommitAsync();

            // 8. Return the newly created account information.
            return RegisterResult.Succeeded(new RegisterResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = role
            });
        }

        public async Task<List<ApiError>> ChangePasswordAsync(
            string userId,
            ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || !user.IsActive)
            {
                return new List<ApiError>
                {
                    new() { Field = string.Empty, Message = "Account not found." }
                };
            }

            // Identity checks the current password and the password rules.
            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword);

            return result.Succeeded
                ? new List<ApiError>()
                : ToApiErrors(result);
        }

        // Roles an Admin can create (Admins are not created through this endpoint).
        private static readonly string[] AllowedRegisterRoles =
        {
            "Doctor",
            "Nurse",
            "Caregiver",
            "Patient"
        };

        // Turns Identity errors into the API's error format,
        // guessing the field from the Identity error code.
        private static List<ApiError> ToApiErrors(IdentityResult result)
        {
            return result.Errors
                .Select(e => new ApiError
                {
                    Field = e.Code.Contains("Password") ? "Password"
                        : e.Code.Contains("Email") ? "Email"
                        : e.Code.Contains("UserName") ? "UserName"
                        : e.Code.Contains("Role") ? "Role"
                        : string.Empty,
                    Message = e.Description
                })
                .ToList();
        }
    }
}