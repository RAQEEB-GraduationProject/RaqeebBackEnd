using System.Security.Claims;

namespace RAQEEB.Common
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User =>
            _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated =>
            User?.Identity?.IsAuthenticated ?? false;

        public string? UserId =>
            User?.FindFirstValue(
                ClaimTypes.NameIdentifier);

        public string? UserName =>
            User?.FindFirstValue(
                ClaimTypes.Name);

        public Guid? HospitalId
        {
            get
            {
                var value = User?.FindFirstValue("HospitalId");

                if (Guid.TryParse(value, out var hospitalId))
                {
                    return hospitalId;
                }

                return null;
            }
        }

        public string? Role =>
            User?.FindFirstValue(
                ClaimTypes.Role);
    }
}