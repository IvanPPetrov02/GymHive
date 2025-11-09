using GymService.BLL.Entities;
using GymService.BLL.RepositoryInterfaces;
using GymService.DAL.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace GymService.DAL.Repositories;

public class GymRepository : IGymRepository
{
    private readonly GymDbContext _context;

    public GymRepository(GymDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Gym>> GetAllAsync()
    {
        return await _context.Gyms
            .Include(g => g.GymGroups)
            .ToListAsync();
    }

    public async Task<Gym?> GetByIdAsync(int id)
    {
        return await _context.Gyms
            .Include(g => g.GymGroups)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Gym> CreateAsync(Gym gym)
    {
        _context.Gyms.Add(gym);
        await _context.SaveChangesAsync();
        return gym;
    }

    public async Task<Gym?> UpdateAsync(Gym gym)
    {
        _context.Gyms.Update(gym);
        await _context.SaveChangesAsync();
        return gym;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var gym = await _context.Gyms.FindAsync(id);
        if (gym == null) return false;

        _context.Gyms.Remove(gym);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Gyms.AnyAsync(g => g.Id == id);
    }
}
