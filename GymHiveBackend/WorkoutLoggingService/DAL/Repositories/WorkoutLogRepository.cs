using Microsoft.EntityFrameworkCore;
using WorkoutLoggingService.DAL.DbContexts;
using WorkoutLoggingService.DAL.Entities;
using WorkoutLoggingService.DAL.RepositoryInterfaces;

namespace WorkoutLoggingService.DAL.Repositories;

public class WorkoutLogRepository : IWorkoutLogRepository
{
    private readonly WorkoutLoggingDbContext _context;

    public WorkoutLogRepository(WorkoutLoggingDbContext context)
    {
        _context = context;
    }

    public async Task<List<WorkoutLog>> GetUserWorkoutLogsAsync(Guid userId, int skip = 0, int take = 20)
    {
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<WorkoutLog?> GetByIdAsync(int id)
    {
        return await _context.WorkoutLogs.FindAsync(id);
    }

    public async Task<WorkoutLog?> GetActiveCheckInAsync(Guid userId)
    {
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId && w.CheckOutTime == null)
            .OrderByDescending(w => w.CheckInTime)
            .FirstOrDefaultAsync();
    }

    public async Task<WorkoutLog> CreateAsync(WorkoutLog workoutLog)
    {
        _context.WorkoutLogs.Add(workoutLog);
        await _context.SaveChangesAsync();
        return workoutLog;
    }

    public async Task<bool> UpdateAsync(WorkoutLog workoutLog)
    {
        _context.WorkoutLogs.Update(workoutLog);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> GetTotalWorkoutsAsync(Guid userId)
    {
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId && w.CheckOutTime != null)
            .CountAsync();
    }

    public async Task<int> GetTotalMinutesAsync(Guid userId)
    {
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId && w.Duration != null)
            .SumAsync(w => w.Duration ?? 0);
    }

    public async Task<DateTime?> GetLastWorkoutAsync(Guid userId)
    {
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId && w.CheckOutTime != null)
            .OrderByDescending(w => w.CheckOutTime)
            .Select(w => w.CheckOutTime)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetWorkoutsThisWeekAsync(Guid userId)
    {
        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId && w.CheckOutTime != null && w.CheckOutTime >= startOfWeek)
            .CountAsync();
    }

    public async Task<int> GetWorkoutsThisMonthAsync(Guid userId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _context.WorkoutLogs
            .Where(w => w.UserId == userId && w.CheckOutTime != null && w.CheckOutTime >= startOfMonth)
            .CountAsync();
    }
}
