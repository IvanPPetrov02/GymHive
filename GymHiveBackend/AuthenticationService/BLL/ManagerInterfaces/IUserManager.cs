namespace BLL.ManagerInterfaces;

using BLL.Entities;
using BLL.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IUserManager
{
    Task<string> RegisterUserAsync(UserRegisterDTO userDto);
    Task<string?> AuthenticateUserAsync(string email, string password);
    Task UpdateUserDetailsAsync(string uuid, UserUpdateDTO userDto);
    Task DeleteUserAsync(string uuid);
    Task<User?> GetUserByIdAsync(string uuid);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task ActivateOrDeactivateUserAsync(string uuid, bool isActive);
    Task<User?> GetUserByEmailAsync(string email);
    Task ChangePasswordAsync(string uuid, string newPassword, string oldPassword);
    Task<User?> GetLoggedUserAsync(string jwt);
    Task UpdateUserRoleAsync(string uuid, Role role);
    Task UpdateUserGymIdAsync(string uuid, int? gymId);
}
