using WorkoutLoggingService.BLL.Entities;

namespace WorkoutLoggingService.BLL.RepositoryInterfaces;

public interface IWorkoutLogRepository
{
    Task<WorkoutLog?> GetByIdAsync(int id);
    Task<List<WorkoutLog>> GetUserVisitsByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<WorkoutLog> AddAsync(WorkoutLog workoutLog);
    Task DeleteAsync(int id);
    Task<bool> HasVisitOnDateAsync(Guid userId, int gymId, DateTime date);
}
