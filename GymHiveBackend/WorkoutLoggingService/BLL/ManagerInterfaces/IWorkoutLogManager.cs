using WorkoutLoggingService.BLL.DTOs;

namespace WorkoutLoggingService.BLL.ManagerInterfaces;

public interface IWorkoutLogManager
{
    Task<List<WorkoutLogDTO>> GetUserWorkoutLogsAsync(Guid userId, int skip = 0, int take = 20);
    Task<WorkoutLogDTO> CheckInAsync(Guid userId, int gymId);
    Task<WorkoutLogDTO> CheckOutAsync(Guid userId, int workoutLogId);
    Task<WorkoutStatsDTO> GetWorkoutStatsAsync(Guid userId);
}
