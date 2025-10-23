namespace GymService.Services;

/// <summary>
/// Extracts user information from HTTP headers set by the API Gateway
/// This replaces JWT token parsing - the gateway validates tokens and adds user info to headers
/// </summary>
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
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("X-User-Id header is missing or empty");
            throw new UnauthorizedAccessException("User ID not found in request");
        }

        if (!Guid.TryParse(userId, out var guid))
        {
            _logger.LogError("Invalid X-User-Id format: {UserId}", userId);
            throw new UnauthorizedAccessException("Invalid user ID format");
        }

        return guid;
    }

    public string GetCurrentUserEmail()
    {
        var email = GetHeaderValue("X-User-Email");
        
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("X-User-Email header is missing or empty");
            throw new UnauthorizedAccessException("User email not found in request");
        }

        return email;
    }

    public string GetCurrentUserRole()
    {
        var role = GetHeaderValue("X-User-Role");
        
        if (string.IsNullOrEmpty(role))
        {
            _logger.LogWarning("X-User-Role header is missing or empty");
            throw new UnauthorizedAccessException("User role not found in request");
        }

        return role;
    }

    public bool IsInRole(string role)
    {
        try
        {
            var userRole = GetCurrentUserRole();
            return userRole.Equals(role, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string? GetHeaderValue(string headerName)
    {
        var context = _httpContextAccessor.HttpContext;
        
        if (context == null)
        {
            _logger.LogError("HttpContext is null");
            return null;
        }

        if (context.Request.Headers.TryGetValue(headerName, out var values))
        {
            return values.FirstOrDefault();
        }

        return null;
    }
}
