using Microsoft.EntityFrameworkCore;
using WorkoutLoggingService.BLL.Entities;
using WorkoutLoggingService.BLL.RepositoryInterfaces;
using WorkoutLoggingService.DAL.DbContexts;

namespace WorkoutLoggingService.DAL.Repositories;

public class WorkoutLogRepository : IWorkoutLogRepository
{
    private readonly WorkoutLoggingDbContext _context;

    public WorkoutLogRepository(WorkoutLoggingDbContext context)
    {
        _context = context;
    }

    public async Task<WorkoutLog?> GetByIdAsync(int id)
    {
        return await _context.WorkoutLogs.FindAsync(id);
    }

    public async Task<List<WorkoutLog>> GetUserVisitsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId 
                     && w.VisitDate.Date >= startDate.Date
                     && w.VisitDate.Date <= endDate.Date)
            .OrderBy(w => w.VisitDate)
            .ToListAsync();
    }

    public async Task<WorkoutLog> AddAsync(WorkoutLog workoutLog)
    {
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();
        return workoutLog;
    }

    public async Task DeleteAsync(int id)
    {
        var workoutLog = await GetByIdAsync(id);
        if (workoutLog != null)
        {
            _context.WorkoutLogs.Remove(workoutLog);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasVisitOnDateAsync(Guid userId, int gymId, DateTime date)
    {
        return await _context.WorkoutLogs
            .AnyAsync(w => w.UserId == userId 
                        && w.GymId == gymId 
                        && w.VisitDate.Date == date.Date);
    }
}
