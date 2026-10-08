using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAQEEB.Common;

namespace RAQEEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ICurrentUser _currentUser;

        public TestController(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                isAuthenticated = _currentUser.IsAuthenticated,
                userId = _currentUser.UserId,
                userName = _currentUser.UserName,
                role = _currentUser.Role,
                hospitalId = _currentUser.HospitalId
            });
        }
    }
}