using WorkoutLoggingService.BLL.DTOs;

namespace WorkoutLoggingService.BLL.ManagerInterfaces;

public interface IWorkoutLogManager
{
    Task<List<GymVisitDTO>> GetGymVisitsAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<GymVisitDTO> LogGymVisitAsync(Guid userId, LogGymVisitDTO dto);
    Task DeleteGymVisitAsync(Guid userId, int visitId);
}
