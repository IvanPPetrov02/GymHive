using MembershipService.BLL.Entities;
using MembershipService.BLL.RepositoryInterfaces;
using MembershipService.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace MembershipService.DAL.Repositories;

public class MembershipRepository : IMembershipRepository
{
    private readonly MembershipDbContext _context;

    public MembershipRepository(MembershipDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Membership>> GetAllAsync()
    {
        return await _context.Memberships.ToListAsync();
    }

    public async Task<Membership?> GetByIdAsync(string id)
    {
        return await _context.Memberships.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Membership>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Memberships
            .Where(m => m.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Membership>> GetByGymIdAsync(int gymId)
    {
        return await _context.Memberships
            .Where(m => m.GymId == gymId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Membership>> GetExpiringMembershipsAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Memberships
            .Where(m => m.EndDate >= startDate && m.EndDate < endDate && m.IsActive)
            .ToListAsync();
    }

    public async Task<Membership> CreateAsync(Membership membership)
    {
        // Generate ObjectId if not provided
        if (string.IsNullOrEmpty(membership.Id))
        {
            membership.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        }
        
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

    public async Task<bool> DeleteAsync(string id)
    {
        var membership = await _context.Memberships.FirstOrDefaultAsync(m => m.Id == id);
        if (membership == null) return false;

        _context.Memberships.Remove(membership);
        await _context.SaveChangesAsync();
        return true;
    }
}
