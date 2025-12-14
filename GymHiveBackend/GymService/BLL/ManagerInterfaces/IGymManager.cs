using GymService.BLL.DTOs;

namespace GymService.BLL.ManagerInterfaces;

public interface IGymManager
{
    Task<IEnumerable<GymDTO>> GetAllGymsAsync();
    Task<GymDTO?> GetGymByIdAsync(int id);
    Task<GymDTO> CreateGymAsync(CreateGymDTO createGymDto);
    Task<GymDTO?> UpdateGymAsync(int id, UpdateGymDTO updateGymDto);
    Task<bool> DeleteGymAsync(int id);
}
