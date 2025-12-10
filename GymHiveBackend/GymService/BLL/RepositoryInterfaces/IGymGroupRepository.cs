using GymService.BLL.Entities;

namespace GymService.BLL.RepositoryInterfaces;

public interface IGymGroupRepository
{
    Task<IEnumerable<GymGroup>> GetAllAsync();
    Task<GymGroup?> GetByIdAsync(int id);
    Task<IEnumerable<GymGroup>> GetByGymIdAsync(int gymId);
    Task<IEnumerable<GymGroup>> GetByModeratorIdAsync(Guid moderatorId);
    Task<GymGroup> CreateAsync(GymGroup gymGroup);
    Task<GymGroup?> UpdateAsync(GymGroup gymGroup);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<GymGroupMember>> GetGroupMembersAsync(int groupId);
    Task AddMemberAsync(GymGroupMember member);
    Task RemoveMemberByUserIdAsync(int groupId, Guid userId);
}
