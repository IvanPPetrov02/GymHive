using MembershipService.BLL.Entities;

namespace MembershipService.BLL.RepositoryInterfaces;

public interface IMembershipRepository
{
    Task<IEnumerable<Membership>> GetAllAsync();
    Task<Membership?> GetByIdAsync(string id);
    Task<IEnumerable<Membership>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Membership>> GetByGymIdAsync(int gymId);
    Task<IEnumerable<Membership>> GetExpiringMembershipsAsync(DateTime startDate, DateTime endDate);
    Task<Membership> CreateAsync(Membership membership);
    Task<Membership> UpdateAsync(Membership membership);
    Task<bool> DeleteAsync(string id);
}
