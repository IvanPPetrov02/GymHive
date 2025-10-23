using GymService.BLL.DTOs;

namespace GymService.BLL.ManagerInterfaces;

public interface IGymGroupManager
{
    Task<IEnumerable<GymGroupDTO>> GetAllGymGroupsAsync();
    Task<GymGroupDTO?> GetGymGroupByIdAsync(int id);
    Task<IEnumerable<GymGroupDTO>> GetGymGroupsByGymIdAsync(int gymId);
    Task<IEnumerable<GymGroupDTO>> GetGymGroupsByModeratorIdAsync(int moderatorId);
    Task<GymGroupDTO> CreateGymGroupAsync(CreateGymGroupDTO createGymGroupDto);
    Task<GymGroupDTO?> UpdateGymGroupAsync(int id, UpdateGymGroupDTO updateGymGroupDto);
    Task<bool> DeleteGymGroupAsync(int id);
}
