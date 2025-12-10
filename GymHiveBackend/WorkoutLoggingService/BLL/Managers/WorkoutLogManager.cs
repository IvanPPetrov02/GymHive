using WorkoutLoggingService.BLL.DTOs;
using WorkoutLoggingService.BLL.Entities;
using WorkoutLoggingService.BLL.ManagerInterfaces;
using WorkoutLoggingService.BLL.RepositoryInterfaces;

namespace WorkoutLoggingService.BLL.Managers;

public class WorkoutLogManager : IWorkoutLogManager
{
    private readonly IWorkoutLogRepository _repository;

    public WorkoutLogManager(IWorkoutLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<GymVisitDTO>> GetGymVisitsAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        var visits = await _repository.GetUserVisitsByDateRangeAsync(userId, startDate, endDate);
        
        return visits.Select(v => new GymVisitDTO
        {
            Id = v.Id,
            GymId = v.GymId,
            VisitDate = v.VisitDate
        }).ToList();
    }

    public async Task<GymVisitDTO> LogGymVisitAsync(Guid userId, LogGymVisitDTO dto)
    {
        var today = DateTime.Today;
        
        // Check if already logged for this gym today
        if (await _repository.HasVisitOnDateAsync(userId, dto.GymId, today))
        {
            throw new InvalidOperationException("You have already logged a visit to this gym today");
        }

        var workoutLog = new WorkoutLog
        {
            UserId = userId,
            GymId = dto.GymId,
            VisitDate = today
        };

        var created = await _repository.AddAsync(workoutLog);

        return new GymVisitDTO
        {
            Id = created.Id,
            GymId = created.GymId,
            VisitDate = created.VisitDate
        };
    }

    public async Task DeleteGymVisitAsync(Guid userId, int visitId)
    {
        var visit = await _repository.GetByIdAsync(visitId);
        
        if (visit == null)
        {
            throw new KeyNotFoundException("Gym visit not found");
        }

        if (visit.UserId != userId)
        {
            throw new UnauthorizedAccessException("You can only delete your own gym visits");
        }

        await _repository.DeleteAsync(visitId);
    }
}
