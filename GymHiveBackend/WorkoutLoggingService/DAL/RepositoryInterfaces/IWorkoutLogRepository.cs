using WorkoutLoggingService.DAL.Entities;

namespace WorkoutLoggingService.DAL.RepositoryInterfaces;

public interface IWorkoutLogRepository
{
    Task<List<WorkoutLog>> GetUserWorkoutLogsAsync(Guid userId, int skip = 0, int take = 20);
    Task<WorkoutLog?> GetByIdAsync(int id);
    Task<WorkoutLog?> GetActiveCheckInAsync(Guid userId);
    Task<WorkoutLog> CreateAsync(WorkoutLog workoutLog);
    Task<bool> UpdateAsync(WorkoutLog workoutLog);
    Task<int> GetTotalWorkoutsAsync(Guid userId);
    Task<int> GetTotalMinutesAsync(Guid userId);
    Task<DateTime?> GetLastWorkoutAsync(Guid userId);
    Task<int> GetWorkoutsThisWeekAsync(Guid userId);
    Task<int> GetWorkoutsThisMonthAsync(Guid userId);
}
