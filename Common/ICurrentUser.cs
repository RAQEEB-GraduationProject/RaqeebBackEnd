namespace RAQEEB.Common
{
    public interface ICurrentUser
    {
        string? UserId { get; }

        string? UserName { get; }

        Guid? HospitalId { get; }

        string? Role { get; }

        bool IsAuthenticated { get; }
    }
}