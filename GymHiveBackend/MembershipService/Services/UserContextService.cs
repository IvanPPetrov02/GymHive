namespace MembershipService.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in request headers");
        }
        return userId;
    }

    public string GetCurrentUserRole()
    {
        var roleClaim = _httpContextAccessor.HttpContext?.Request.Headers["X-User-Role"].FirstOrDefault();
        return roleClaim ?? "User";
    }

    public bool IsInRole(string role)
    {
        var currentRole = GetCurrentUserRole();
        return currentRole.Equals(role, StringComparison.OrdinalIgnoreCase);
    }
}
