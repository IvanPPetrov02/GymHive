using BLL.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BLL.RepositoryInterfaces;

public interface IUserDAO
{
    Task<User?> GetUserByIdAsync(string uuid);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task CreateUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(string uuid);
}