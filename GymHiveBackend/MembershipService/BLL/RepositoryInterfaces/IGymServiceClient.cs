using MembershipService.BLL.DTOs;

namespace MembershipService.BLL.RepositoryInterfaces;

public interface IGymServiceClient
{
    Task<GymDTO?> GetGymByIdAsync(int gymId);
}
