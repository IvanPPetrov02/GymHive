using GymService.BLL.Entities;

namespace GymService.BLL.RepositoryInterfaces;

public interface IGymRepository
{
    Task<IEnumerable<Gym>> GetAllAsync();
    Task<Gym?> GetByIdAsync(int id);
    Task<Gym> CreateAsync(Gym gym);
    Task<Gym?> UpdateAsync(Gym gym);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
