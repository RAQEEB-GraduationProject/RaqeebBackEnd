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

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password"
                });
            }

            return Ok(
                ApiResponse<object>.SuccessResponse( // object is used here because the response type is not specified in the ApiResponse class
                    result,
                    "Login successful."
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
                    ApiResponse<object>.FailureResponse(
                        "HOSPITAL_NOT_ASSIGNED",
                        "Current user is not assigned to a hospital."
                    )
                );
            }

            var success = await _authService.RegisterAsync(
                registerDto,
                _currentUser.HospitalId.Value);

            if (!success)
            {
                return BadRequest(
                    ApiResponse<object>.FailureResponse(
                        "REGISTRATION_FAILED",
                        "User registration failed."
                    )
                );
            }

            return Ok(
                ApiResponse<object?>.SuccessResponse(
                    null,
                    "User registered successfully."
                )
            );
        }
    }
}