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

    public async Task<IEnumerable<GymGroup>> GetByModeratorIdAsync(Guid moderatorId)
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

    public async Task<IEnumerable<GymGroupMember>> GetGroupMembersAsync(int groupId)
    {
        return await _context.GymGroupMembers
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task AddMemberAsync(GymGroupMember member)
    {
        // Check if member already exists
        var existing = await _context.GymGroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == member.GroupId && m.UserId == member.UserId);
        
        if (existing == null)
        {
            _context.GymGroupMembers.Add(member);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveMemberByUserIdAsync(int groupId, Guid userId)
    {
        var member = await _context.GymGroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        
        if (member != null)
        {
            _context.GymGroupMembers.Remove(member);
            await _context.SaveChangesAsync();
        }
    }
}
