using GymService.BLL.DTOs;

namespace GymService.BLL.ManagerInterfaces;

public interface IGymGroupManager
{
    Task<IEnumerable<GymGroupDTO>> GetAllGymGroupsAsync();
    Task<GymGroupDTO?> GetGymGroupByIdAsync(int id);
    Task<IEnumerable<GymGroupDTO>> GetGymGroupsByGymIdAsync(int gymId);
    Task<IEnumerable<GymGroupDTO>> GetGymGroupsByModeratorIdAsync(Guid moderatorId);
    Task<GymGroupDTO> CreateGymGroupAsync(CreateGymGroupDTO createGymGroupDto);
    Task<GymGroupDTO?> UpdateGymGroupAsync(int id, UpdateGymGroupDTO updateGymGroupDto);
    Task<bool> DeleteGymGroupAsync(int id);
    Task<IEnumerable<GymGroupMemberDTO>> GetGroupMembersAsync(int groupId);
    Task AddMemberAsync(int groupId, Guid userId);
    Task RemoveMemberByUserIdAsync(int groupId, Guid userId);
}
