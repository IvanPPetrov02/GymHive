using GymService.BLL.Entities;
using GymService.BLL.RepositoryInterfaces;
using GymService.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace GymService.DAL.Repositories;

public class GymGroupRepository : IGymGroupRepository
{
    private readonly GymDbContext _context;

    public GymGroupRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GymGroup>> GetAllAsync()
    {
        return await _context.GymGroups
            .Include(gg => gg.Gym)
            .ToListAsync();
    }

    public async Task<GymGroup?> GetByIdAsync(int id)
    {
        return await _context.GymGroups
            .Include(gg => gg.Gym)
            .FirstOrDefaultAsync(gg => gg.Id == id);
    }

    public async Task<IEnumerable<GymGroup>> GetByGymIdAsync(int gymId)
    {
        return await _context.GymGroups
            .Include(gg => gg.Gym)
            .Where(gg => gg.GymId == gymId)
            .ToListAsync();
    }

    public async Task<IEnumerable<GymGroup>> GetByModeratorIdAsync(int moderatorId)
    {
        return await _context.GymGroups
            .Include(gg => gg.Gym)
            .Where(gg => gg.ModeratorId == moderatorId)
            .ToListAsync();
    }

    public async Task<GymGroup> CreateAsync(GymGroup gymGroup)
    {
        _context.GymGroups.Add(gymGroup);
        await _context.SaveChangesAsync();
        return gymGroup;
    }

    public async Task<GymGroup?> UpdateAsync(GymGroup gymGroup)
    {
        _context.GymGroups.Update(gymGroup);
        await _context.SaveChangesAsync();
        return gymGroup;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var gymGroup = await _context.GymGroups.FindAsync(id);
        if (gymGroup == null) return false;

        _context.GymGroups.Remove(gymGroup);
        await _context.SaveChangesAsync();
        return true;
    }
}
