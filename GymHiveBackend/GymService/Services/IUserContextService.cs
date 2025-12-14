namespace GymService.Services;

/// <summary>
/// Service to extract user information from request headers
/// These headers are added by the API Gateway after token validation
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Gets the current user's ID from the request headers
    /// </summary>
    Guid GetCurrentUserId();

    /// <summary>
    /// Gets the current user's email from the request headers
    /// </summary>
    string GetCurrentUserEmail();

    /// <summary>
    /// Gets the current user's role from the request headers
    /// </summary>
    string GetCurrentUserRole();

    /// <summary>
    /// Checks if the current user has the specified role
    /// </summary>
    bool IsInRole(string role);

    /// <summary>
    /// Gets the current user's gym ID from the request headers (for moderators)
    /// </summary>
    int? GetCurrentUserGymId();
}
