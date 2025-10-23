using BLL.Entities;
using BLL.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;
using DAL.DbContexts;

namespace AuthenticationService.DAL.Repositories;

public class UserRepository : IUserDAO, IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // IUserRepository methods
    public async Task AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    // IUserDAO methods
    public async Task<User?> GetUserByIdAsync(string uuid)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UUID.ToString() == uuid);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task CreateUserAsync(User user)
    {
        await AddUserAsync(user);
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(string uuid)
    {
        var user = await GetUserByIdAsync(uuid);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
