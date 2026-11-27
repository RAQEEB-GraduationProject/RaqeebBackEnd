using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAQEEB.Common;
using RAQEEB.DTOs.Common;
using RAQEEB.DTOs.Auth;
using RAQEEB.Services.Auth;

namespace RAQEEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUser _currentUser;

        public AuthController(
            IAuthService authService,
            ICurrentUser currentUser)
        {
            _authService = authService;
            _currentUser = currentUser;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto loginDto)
        {
            var result =
                await _authService.LoginAsync(loginDto);

            if (result.Status == LoginStatus.LockedOut)
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    ApiResponse<LoginResponseDto>.FailureResponse(
                        "ACCOUNT_LOCKED",
                        "Too many failed login attempts. Try again in 5 minutes."
                    )
                );
            }

            if (result.Status != LoginStatus.Success)
            {
                // Same ApiResponse shape as every other endpoint
                // (it still has a "message" field, like before).
                return Unauthorized(
                    ApiResponse<LoginResponseDto>.FailureResponse(
                        "INVALID_CREDENTIALS",
                        "Invalid username or password."
                    )
                );
            }

            return Ok(
                ApiResponse<LoginResponseDto>.SuccessResponse(
                    result.Data!,
                    "Login successful."
                )
            );
        }

        // Any logged-in user can change their own password.
        // POST: api/Auth/change-password
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordDto dto)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
            {
                return Unauthorized();
            }

            var errors = await _authService.ChangePasswordAsync(userId, dto);

            if (errors.Count > 0)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "CHANGE_PASSWORD_FAILED",
                        "The password could not be changed.",
                        errors
                    )
                );
            }

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    new { },
                    "Password changed successfully."
                )
            );
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
        [FromBody] RegisterDto registerDto)
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Unauthorized();
            }

            if (!_currentUser.HospitalId.HasValue)
            {
                return BadRequest(
                    ApiResponse<RegisterResponseDto>.FailureResponse(
                        "HOSPITAL_NOT_ASSIGNED",
                        "Current user is not assigned to a hospital."
                    )
                );
            }

            var result = await _authService.RegisterAsync(
                registerDto,
                _currentUser.HospitalId.Value);

            return result.Status switch
            {
                RegisterStatus.Success => Ok(
                    ApiResponse<RegisterResponseDto>.SuccessResponse(
                        result.Data!,
                        "User registered successfully.")),

                RegisterStatus.UserNameTaken => Conflict(
                    ApiResponse<RegisterResponseDto>.FailureResponse(
                        "USERNAME_TAKEN",
                        "This username is already taken.",
                        result.Errors)),

                RegisterStatus.EmailTaken => Conflict(
                    ApiResponse<RegisterResponseDto>.FailureResponse(
                        "EMAIL_TAKEN",
                        "This email is already registered.",
                        result.Errors)),

                RegisterStatus.InvalidRole => BadRequest(
                    ApiResponse<RegisterResponseDto>.FailureResponse(
                        "INVALID_ROLE",
                        "The selected role is not allowed.",
                        result.Errors)),

                RegisterStatus.InvalidData => BadRequest(
                    ApiResponse<RegisterResponseDto>.FailureResponse(
                        "VALIDATION_ERROR",
                        "One or more validation errors occurred.",
                        result.Errors)),

                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ApiResponse<RegisterResponseDto>.FailureResponse(
                        "REGISTRATION_FAILED",
                        "User registration failed.",
                        result.Errors))
            };
        }
    }
}