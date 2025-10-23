using GymService.BLL.Entities;
using GymService.BLL.RepositoryInterfaces;
using GymService.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace GymService.DAL.Repositories;

public class MembershipRepository : IMembershipRepository
{
    private readonly GymDbContext _context;

    public MembershipRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Membership>> GetAllAsync()
    {
        return await _context.Memberships
            .Include(m => m.Gym)
            .ToListAsync();
    }

    public async Task<Membership?> GetByIdAsync(int id)
    {
        return await _context.Memberships
            .Include(m => m.Gym)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Membership>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Memberships
            .Include(m => m.Gym)
            .Where(m => m.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Membership>> GetByGymIdAsync(int gymId)
    {
        return await _context.Memberships
            .Include(m => m.Gym)
            .Where(m => m.GymId == gymId)
            .ToListAsync();
    }

    public async Task<Membership> CreateAsync(Membership membership)
    {
        _context.Memberships.Add(membership);
        await _context.SaveChangesAsync();
        return membership;
    }

    public async Task<Membership?> UpdateAsync(Membership membership)
    {
        _context.Memberships.Update(membership);
        await _context.SaveChangesAsync();
        return membership;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var membership = await _context.Memberships.FindAsync(id);
        if (membership == null) return false;

        _context.Memberships.Remove(membership);
        await _context.SaveChangesAsync();
        return true;
    }
}
