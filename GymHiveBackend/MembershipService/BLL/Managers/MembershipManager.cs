using MembershipService.BLL.DTOs;
using MembershipService.BLL.Entities;
using MembershipService.BLL.ManagerInterfaces;
using MembershipService.BLL.RepositoryInterfaces;

namespace MembershipService.BLL.Managers;

public class MembershipManager : IMembershipManager
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IGymServiceClient _gymServiceClient;

    public MembershipManager(IMembershipRepository membershipRepository, IGymServiceClient gymServiceClient)
    {
        _membershipRepository = membershipRepository;
        _gymServiceClient = gymServiceClient;
    }

    public async Task<IEnumerable<MembershipDTO>> GetAllMembershipsAsync()
    {
        var memberships = await _membershipRepository.GetAllAsync();
        return await MapToDTOsAsync(memberships);
    }

    public async Task<MembershipDTO?> GetMembershipByIdAsync(string id)
    {
        var membership = await _membershipRepository.GetByIdAsync(id);
        if (membership == null) return null;

        var gym = await _gymServiceClient.GetGymByIdAsync(membership.GymId);
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
            AutoRenew = membership.AutoRenew,
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
        // Check if user already has an active membership for this gym
        var existingMemberships = await _membershipRepository.GetByUserIdAsync(userId);
        var existingMembership = existingMemberships
            .Where(m => m.GymId == createMembershipDto.GymId && m.IsActive)
            .OrderByDescending(m => m.EndDate)
            .FirstOrDefault();
        
        // If user has existing membership, automatically start new one after it expires
        if (existingMembership != null && existingMembership.EndDate >= createMembershipDto.StartDate)
        {
            var originalStartDate = createMembershipDto.StartDate;
            createMembershipDto.StartDate = existingMembership.EndDate.AddDays(1);
            
            // Adjust end date to maintain the same duration
            var duration = (createMembershipDto.EndDate - originalStartDate).Days;
            createMembershipDto.EndDate = createMembershipDto.StartDate.AddDays(duration);
        }
        
        // Fetch gym details first
        var gym = await _gymServiceClient.GetGymByIdAsync(createMembershipDto.GymId);
        
        var membership = new Membership
        {
            UserId = userId,
            GymId = createMembershipDto.GymId,
            GymName = gym?.Name ?? "Unknown Gym",
            MembershipType = createMembershipDto.MembershipType,
            StartDate = createMembershipDto.StartDate,
            EndDate = createMembershipDto.EndDate,
            Price = createMembershipDto.Price,
            IsActive = true,
            AutoRenew = createMembershipDto.AutoRenew,
            CreatedAt = DateTime.UtcNow
        };

        var createdMembership = await _membershipRepository.CreateAsync(membership);

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
            AutoRenew = createdMembership.AutoRenew,
            Price = createdMembership.Price
        };
    }

    public async Task<MembershipDTO?> UpdateMembershipAsync(string id, UpdateMembershipDTO updateMembershipDto)
    {
        var membership = await _membershipRepository.GetByIdAsync(id);
        if (membership == null) return null;

        if (updateMembershipDto.MembershipType != null) membership.MembershipType = updateMembershipDto.MembershipType;
        if (updateMembershipDto.EndDate.HasValue) membership.EndDate = updateMembershipDto.EndDate.Value;
        if (updateMembershipDto.IsActive.HasValue) membership.IsActive = updateMembershipDto.IsActive.Value;
        if (updateMembershipDto.AutoRenew.HasValue) membership.AutoRenew = updateMembershipDto.AutoRenew.Value;
        membership.UpdatedAt = DateTime.UtcNow;

        var updatedMembership = await _membershipRepository.UpdateAsync(membership);
        if (updatedMembership == null) return null;

        var gym = await _gymServiceClient.GetGymByIdAsync(updatedMembership.GymId);
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
            AutoRenew = updatedMembership.AutoRenew,
            Price = updatedMembership.Price
        };
    }

    public async Task<bool> DeleteMembershipAsync(string id)
    {
        return await _membershipRepository.DeleteAsync(id);
    }

    private async Task<IEnumerable<MembershipDTO>> MapToDTOsAsync(IEnumerable<Membership> memberships)
    {
        var membershipDTOs = new List<MembershipDTO>();
        foreach (var membership in memberships)
        {
            var gym = await _gymServiceClient.GetGymByIdAsync(membership.GymId);
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
                AutoRenew = membership.AutoRenew,
                Price = membership.Price
            });
        }
        return membershipDTOs;
    }
}
