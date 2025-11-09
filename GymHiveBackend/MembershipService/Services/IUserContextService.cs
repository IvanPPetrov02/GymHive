namespace MembershipService.Services;

public interface IUserContextService
{
    Guid GetCurrentUserId();
    string GetCurrentUserRole();
    bool IsInRole(string role);
}
