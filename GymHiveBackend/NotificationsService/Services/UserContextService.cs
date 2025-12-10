namespace NotificationsService.Services;

public interface IUserContextService
{
    Guid GetCurrentUserId();
    string? GetCurrentUserEmail();
    string? GetCurrentUserRole();
    bool IsInRole(string role);
}

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UserContextService> _logger;

    public UserContextService(IHttpContextAccessor httpContextAccessor, ILogger<UserContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Guid GetCurrentUserId()
    {
        var userId = GetHeaderValue("X-User-Id");
        
        _logger.LogInformation($"[UserContext] X-User-Id header: {userId}");
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("[UserContext] User ID not found in request headers");
            throw new UnauthorizedAccessException("User ID not found in request");
        }

        if (!Guid.TryParse(userId, out var guid))
        {
            _logger.LogError($"[UserContext] Invalid user ID format: {userId}");
            throw new UnauthorizedAccessException("Invalid user ID format");
        }

        return guid;
    }

    public string? GetCurrentUserEmail()
    {
        var email = GetHeaderValue("X-User-Email");
        _logger.LogInformation($"[UserContext] X-User-Email header: {email}");
        return email;
    }

    public string? GetCurrentUserRole()
    {
        var role = GetHeaderValue("X-User-Role");
        _logger.LogInformation($"[UserContext] X-User-Role header: {role}");
        return role;
    }

    public bool IsInRole(string role)
    {
        var userRole = GetCurrentUserRole();
        return !string.IsNullOrEmpty(userRole) && userRole.Equals(role, StringComparison.OrdinalIgnoreCase);
    }

    private string? GetHeaderValue(string headerName)
    {
        var headers = _httpContextAccessor.HttpContext?.Request.Headers;
        if (headers != null && headers.TryGetValue(headerName, out var value))
        {
            return value.ToString();
        }
        return null;
    }
}
