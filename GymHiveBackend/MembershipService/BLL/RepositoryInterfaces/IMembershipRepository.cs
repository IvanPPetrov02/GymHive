using MembershipService.BLL.Entities;

namespace MembershipService.BLL.RepositoryInterfaces;

public interface IMembershipRepository
{
    Task<IEnumerable<Membership>> GetAllAsync();
    Task<Membership?> GetByIdAsync(int id);
    Task<IEnumerable<Membership>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Membership>> GetByGymIdAsync(int gymId);
    Task<Membership> CreateAsync(Membership membership);
    Task<Membership?> UpdateAsync(Membership membership);
    Task<bool> DeleteAsync(int id);
}
