using GymService.BLL.DTOs;
using GymService.BLL.Entities;
using GymService.BLL.ManagerInterfaces;
using GymService.BLL.RepositoryInterfaces;

namespace GymService.BLL.Managers;

public class GymManager : IGymManager
{
    private readonly IGymRepository _gymRepository;

    public GymManager(IGymRepository gymRepository)
    {
        _gymRepository = gymRepository;
    }

    public async Task<IEnumerable<GymDTO>> GetAllGymsAsync()
    {
        var gyms = await _gymRepository.GetAllAsync();
        return gyms.Select(g => new GymDTO
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            Address = g.Address,
            City = g.City,
            Country = g.Country,
            Phone = g.Phone,
            Email = g.Email
        });
    }

    public async Task<GymDTO?> GetGymByIdAsync(int id)
    {
        var gym = await _gymRepository.GetByIdAsync(id);
        if (gym == null) return null;

        return new GymDTO
        {
            Id = gym.Id,
            Name = gym.Name,
            Description = gym.Description,
            Address = gym.Address,
            City = gym.City,
            Country = gym.Country,
            Phone = gym.Phone,
            Email = gym.Email
        };
    }

    public async Task<GymDTO> CreateGymAsync(CreateGymDTO createGymDto)
    {
        var gym = new Gym
        {
            Name = createGymDto.Name,
            Description = createGymDto.Description,
            Address = createGymDto.Address,
            City = createGymDto.City,
            Country = createGymDto.Country,
            Phone = createGymDto.Phone,
            Email = createGymDto.Email,
            CreatedAt = DateTime.UtcNow
        };

        var createdGym = await _gymRepository.CreateAsync(gym);

        return new GymDTO
        {
            Id = createdGym.Id,
            Name = createdGym.Name,
            Description = createdGym.Description,
            Address = createdGym.Address,
            City = createdGym.City,
            Country = createdGym.Country,
            Phone = createdGym.Phone,
            Email = createdGym.Email
        };
    }

    public async Task<GymDTO?> UpdateGymAsync(int id, UpdateGymDTO updateGymDto)
    {
        var gym = await _gymRepository.GetByIdAsync(id);
        if (gym == null) return null;

        if (updateGymDto.Name != null) gym.Name = updateGymDto.Name;
        if (updateGymDto.Description != null) gym.Description = updateGymDto.Description;
        if (updateGymDto.Address != null) gym.Address = updateGymDto.Address;
        if (updateGymDto.City != null) gym.City = updateGymDto.City;
        if (updateGymDto.Country != null) gym.Country = updateGymDto.Country;
        if (updateGymDto.Phone != null) gym.Phone = updateGymDto.Phone;
        if (updateGymDto.Email != null) gym.Email = updateGymDto.Email;
        gym.UpdatedAt = DateTime.UtcNow;

        var updatedGym = await _gymRepository.UpdateAsync(gym);
        if (updatedGym == null) return null;

        return new GymDTO
        {
            Id = updatedGym.Id,
            Name = updatedGym.Name,
            Description = updatedGym.Description,
            Address = updatedGym.Address,
            City = updatedGym.City,
            Country = updatedGym.Country,
            Phone = updatedGym.Phone,
            Email = updatedGym.Email
        };
    }

    public async Task<bool> DeleteGymAsync(int id)
    {
        return await _gymRepository.DeleteAsync(id);
    }
}
