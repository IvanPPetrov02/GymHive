using GymService.BLL.DTOs;

namespace GymService.BLL.ManagerInterfaces;

public interface IMembershipManager
{
    Task<IEnumerable<MembershipDTO>> GetAllMembershipsAsync();
    Task<MembershipDTO?> GetMembershipByIdAsync(int id);
    Task<IEnumerable<MembershipDTO>> GetMembershipsByUserIdAsync(Guid userId);
    Task<IEnumerable<MembershipDTO>> GetMembershipsByGymIdAsync(int gymId);
    Task<MembershipDTO> CreateMembershipAsync(Guid userId, CreateMembershipDTO createMembershipDto);
    Task<MembershipDTO?> UpdateMembershipAsync(int id, UpdateMembershipDTO updateMembershipDto);
    Task<bool> DeleteMembershipAsync(int id);
}
