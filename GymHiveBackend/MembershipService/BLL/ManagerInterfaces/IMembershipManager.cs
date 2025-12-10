using MembershipService.BLL.DTOs;

namespace MembershipService.BLL.ManagerInterfaces;

public interface IMembershipManager
{
    Task<IEnumerable<MembershipDTO>> GetAllMembershipsAsync();
    Task<MembershipDTO?> GetMembershipByIdAsync(string id);
    Task<IEnumerable<MembershipDTO>> GetMembershipsByUserIdAsync(Guid userId);
    Task<IEnumerable<MembershipDTO>> GetMembershipsByGymIdAsync(int gymId);
    Task<MembershipDTO> CreateMembershipAsync(Guid userId, CreateMembershipDTO createMembershipDto);
    Task<MembershipDTO?> UpdateMembershipAsync(string id, UpdateMembershipDTO updateMembershipDto);
    Task<bool> DeleteMembershipAsync(string id);
}
