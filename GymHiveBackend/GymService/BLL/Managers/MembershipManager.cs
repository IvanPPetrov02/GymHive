using GymService.BLL.DTOs;
using GymService.BLL.Entities;
using GymService.BLL.ManagerInterfaces;
using GymService.BLL.RepositoryInterfaces;

namespace GymService.BLL.Managers;

public class MembershipManager : IMembershipManager
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IGymRepository _gymRepository;

    public MembershipManager(IMembershipRepository membershipRepository, IGymRepository gymRepository)
    {
        _membershipRepository = membershipRepository;
        _gymRepository = gymRepository;
    }

    public async Task<IEnumerable<MembershipDTO>> GetAllMembershipsAsync()
    {
        var memberships = await _membershipRepository.GetAllAsync();
        return await MapToDTOsAsync(memberships);
    }

    public async Task<MembershipDTO?> GetMembershipByIdAsync(int id)
    {
        var membership = await _membershipRepository.GetByIdAsync(id);
        if (membership == null) return null;

        var gym = await _gymRepository.GetByIdAsync(membership.GymId);
        return new MembershipDTO
        {
            Id = membership.Id,
            UserId = membership.UserId,
            GymId = membership.GymId,
            GymName = gym?.Name ?? "",
            MembershipType = membership.MembershipType,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            IsActive = membership.IsActive,
            Price = membership.Price
        };
    }

    public async Task<IEnumerable<MembershipDTO>> GetMembershipsByUserIdAsync(Guid userId)
    {
        var memberships = await _membershipRepository.GetByUserIdAsync(userId);
        return await MapToDTOsAsync(memberships);
    }

    public async Task<IEnumerable<MembershipDTO>> GetMembershipsByGymIdAsync(int gymId)
    {
        var memberships = await _membershipRepository.GetByGymIdAsync(gymId);
        return await MapToDTOsAsync(memberships);
    }

    public async Task<MembershipDTO> CreateMembershipAsync(Guid userId, CreateMembershipDTO createMembershipDto)
    {
        var membership = new Membership
        {
            UserId = userId,
            GymId = createMembershipDto.GymId,
            MembershipType = createMembershipDto.MembershipType,
            StartDate = createMembershipDto.StartDate,
            EndDate = createMembershipDto.EndDate,
            Price = createMembershipDto.Price,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createdMembership = await _membershipRepository.CreateAsync(membership);
        var gym = await _gymRepository.GetByIdAsync(createdMembership.GymId);

        return new MembershipDTO
        {
            Id = createdMembership.Id,
            UserId = createdMembership.UserId,
            GymId = createdMembership.GymId,
            GymName = gym?.Name ?? "",
            MembershipType = createdMembership.MembershipType,
            StartDate = createdMembership.StartDate,
            EndDate = createdMembership.EndDate,
            IsActive = createdMembership.IsActive,
            Price = createdMembership.Price
        };
    }

    public async Task<MembershipDTO?> UpdateMembershipAsync(int id, UpdateMembershipDTO updateMembershipDto)
    {
        var membership = await _membershipRepository.GetByIdAsync(id);
        if (membership == null) return null;

        if (updateMembershipDto.MembershipType != null) membership.MembershipType = updateMembershipDto.MembershipType;
        if (updateMembershipDto.EndDate.HasValue) membership.EndDate = updateMembershipDto.EndDate.Value;
        if (updateMembershipDto.IsActive.HasValue) membership.IsActive = updateMembershipDto.IsActive.Value;
        membership.UpdatedAt = DateTime.UtcNow;

        var updatedMembership = await _membershipRepository.UpdateAsync(membership);
        if (updatedMembership == null) return null;

        var gym = await _gymRepository.GetByIdAsync(updatedMembership.GymId);
        return new MembershipDTO
        {
            Id = updatedMembership.Id,
            UserId = updatedMembership.UserId,
            GymId = updatedMembership.GymId,
            GymName = gym?.Name ?? "",
            MembershipType = updatedMembership.MembershipType,
            StartDate = updatedMembership.StartDate,
            EndDate = updatedMembership.EndDate,
            IsActive = updatedMembership.IsActive,
            Price = updatedMembership.Price
        };
    }

    public async Task<bool> DeleteMembershipAsync(int id)
    {
        return await _membershipRepository.DeleteAsync(id);
    }

    private async Task<IEnumerable<MembershipDTO>> MapToDTOsAsync(IEnumerable<Membership> memberships)
    {
        var membershipDTOs = new List<MembershipDTO>();
        foreach (var membership in memberships)
        {
            var gym = await _gymRepository.GetByIdAsync(membership.GymId);
            membershipDTOs.Add(new MembershipDTO
            {
                Id = membership.Id,
                UserId = membership.UserId,
                GymId = membership.GymId,
                GymName = gym?.Name ?? "",
                MembershipType = membership.MembershipType,
                StartDate = membership.StartDate,
                EndDate = membership.EndDate,
                IsActive = membership.IsActive,
                Price = membership.Price
            });
        }
        return membershipDTOs;
    }
}
