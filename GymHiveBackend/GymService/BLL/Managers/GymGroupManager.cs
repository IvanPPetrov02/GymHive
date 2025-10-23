using GymService.BLL.DTOs;
using GymService.BLL.Entities;
using GymService.BLL.ManagerInterfaces;
using GymService.BLL.RepositoryInterfaces;

namespace GymService.BLL.Managers;

public class GymGroupManager : IGymGroupManager
{
    private readonly IGymGroupRepository _gymGroupRepository;
    private readonly IGymRepository _gymRepository;

    public GymGroupManager(IGymGroupRepository gymGroupRepository, IGymRepository gymRepository)
    {
        _gymGroupRepository = gymGroupRepository;
        _gymRepository = gymRepository;
    }

    public async Task<IEnumerable<GymGroupDTO>> GetAllGymGroupsAsync()
    {
        var gymGroups = await _gymGroupRepository.GetAllAsync();
        return await MapToDTOsAsync(gymGroups);
    }

    public async Task<GymGroupDTO?> GetGymGroupByIdAsync(int id)
    {
        var gymGroup = await _gymGroupRepository.GetByIdAsync(id);
        if (gymGroup == null) return null;

        var gym = await _gymRepository.GetByIdAsync(gymGroup.GymId);
        return new GymGroupDTO
        {
            Id = gymGroup.Id,
            GymId = gymGroup.GymId,
            GymName = gym?.Name ?? "",
            Name = gymGroup.Name,
            Description = gymGroup.Description,
            ModeratorId = gymGroup.ModeratorId,
            MaxMembers = gymGroup.MaxMembers,
            Schedule = gymGroup.Schedule
        };
    }

    public async Task<IEnumerable<GymGroupDTO>> GetGymGroupsByGymIdAsync(int gymId)
    {
        var gymGroups = await _gymGroupRepository.GetByGymIdAsync(gymId);
        return await MapToDTOsAsync(gymGroups);
    }

    public async Task<IEnumerable<GymGroupDTO>> GetGymGroupsByModeratorIdAsync(int moderatorId)
    {
        var gymGroups = await _gymGroupRepository.GetByModeratorIdAsync(moderatorId);
        return await MapToDTOsAsync(gymGroups);
    }

    public async Task<GymGroupDTO> CreateGymGroupAsync(CreateGymGroupDTO createGymGroupDto)
    {
        var gymGroup = new GymGroup
        {
            GymId = createGymGroupDto.GymId,
            Name = createGymGroupDto.Name,
            Description = createGymGroupDto.Description,
            ModeratorId = createGymGroupDto.ModeratorId,
            MaxMembers = createGymGroupDto.MaxMembers,
            Schedule = createGymGroupDto.Schedule,
            CreatedAt = DateTime.UtcNow
        };

        var createdGymGroup = await _gymGroupRepository.CreateAsync(gymGroup);
        var gym = await _gymRepository.GetByIdAsync(createdGymGroup.GymId);

        return new GymGroupDTO
        {
            Id = createdGymGroup.Id,
            GymId = createdGymGroup.GymId,
            GymName = gym?.Name ?? "",
            Name = createdGymGroup.Name,
            Description = createdGymGroup.Description,
            ModeratorId = createdGymGroup.ModeratorId,
            MaxMembers = createdGymGroup.MaxMembers,
            Schedule = createdGymGroup.Schedule
        };
    }

    public async Task<GymGroupDTO?> UpdateGymGroupAsync(int id, UpdateGymGroupDTO updateGymGroupDto)
    {
        var gymGroup = await _gymGroupRepository.GetByIdAsync(id);
        if (gymGroup == null) return null;

        if (updateGymGroupDto.Name != null) gymGroup.Name = updateGymGroupDto.Name;
        if (updateGymGroupDto.Description != null) gymGroup.Description = updateGymGroupDto.Description;
        if (updateGymGroupDto.MaxMembers.HasValue) gymGroup.MaxMembers = updateGymGroupDto.MaxMembers.Value;
        if (updateGymGroupDto.Schedule != null) gymGroup.Schedule = updateGymGroupDto.Schedule;
        gymGroup.UpdatedAt = DateTime.UtcNow;

        var updatedGymGroup = await _gymGroupRepository.UpdateAsync(gymGroup);
        if (updatedGymGroup == null) return null;

        var gym = await _gymRepository.GetByIdAsync(updatedGymGroup.GymId);
        return new GymGroupDTO
        {
            Id = updatedGymGroup.Id,
            GymId = updatedGymGroup.GymId,
            GymName = gym?.Name ?? "",
            Name = updatedGymGroup.Name,
            Description = updatedGymGroup.Description,
            ModeratorId = updatedGymGroup.ModeratorId,
            MaxMembers = updatedGymGroup.MaxMembers,
            Schedule = updatedGymGroup.Schedule
        };
    }

    public async Task<bool> DeleteGymGroupAsync(int id)
    {
        return await _gymGroupRepository.DeleteAsync(id);
    }

    private async Task<IEnumerable<GymGroupDTO>> MapToDTOsAsync(IEnumerable<GymGroup> gymGroups)
    {
        var gymGroupDTOs = new List<GymGroupDTO>();
        foreach (var gymGroup in gymGroups)
        {
            var gym = await _gymRepository.GetByIdAsync(gymGroup.GymId);
            gymGroupDTOs.Add(new GymGroupDTO
            {
                Id = gymGroup.Id,
                GymId = gymGroup.GymId,
                GymName = gym?.Name ?? "",
                Name = gymGroup.Name,
                Description = gymGroup.Description,
                ModeratorId = gymGroup.ModeratorId,
                MaxMembers = gymGroup.MaxMembers,
                Schedule = gymGroup.Schedule
            });
        }
        return gymGroupDTOs;
    }
}
