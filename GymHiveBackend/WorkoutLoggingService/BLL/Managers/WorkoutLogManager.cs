using WorkoutLoggingService.BLL.DTOs;
using WorkoutLoggingService.BLL.ManagerInterfaces;
using WorkoutLoggingService.DAL.Entities;
using WorkoutLoggingService.DAL.RepositoryInterfaces;
using GymHive.Messaging.Interfaces;
using GymHive.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace WorkoutLoggingService.BLL.Managers;

public class WorkoutLogManager : IWorkoutLogManager
{
    private readonly IWorkoutLogRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<WorkoutLogManager> _logger;

    public WorkoutLogManager(
        IWorkoutLogRepository repository,
        IEventPublisher eventPublisher,
        ILogger<WorkoutLogManager> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<List<WorkoutLogDTO>> GetUserWorkoutLogsAsync(Guid userId, int skip = 0, int take = 20)
    {
        var logs = await _repository.GetUserWorkoutLogsAsync(userId, skip, take);
        return logs.Select(MapToDTO).ToList();
    }

    public async Task<WorkoutLogDTO> CheckInAsync(Guid userId, int gymId)
    {
        // Check if user already has an active check-in
        var activeCheckIn = await _repository.GetActiveCheckInAsync(userId);
        if (activeCheckIn != null)
        {
            throw new InvalidOperationException("User already has an active check-in. Please check out first.");
        }

        var workoutLog = new WorkoutLog
        {
            UserId = userId,
            GymId = gymId,
            CheckInTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(workoutLog);
        _logger.LogInformation("User {UserId} checked in at gym {GymId}", userId, gymId);
        
        return MapToDTO(created);
    }

    public async Task<WorkoutLogDTO> CheckOutAsync(Guid userId, int workoutLogId)
    {
        var workoutLog = await _repository.GetByIdAsync(workoutLogId);
        
        if (workoutLog == null)
        {
            throw new KeyNotFoundException($"Workout log {workoutLogId} not found");
        }

        if (workoutLog.UserId != userId)
        {
            throw new UnauthorizedAccessException("Cannot check out another user's workout");
        }

        if (workoutLog.CheckOutTime != null)
        {
            throw new InvalidOperationException("Already checked out");
        }

        workoutLog.CheckOutTime = DateTime.UtcNow;
        workoutLog.Duration = (int)(workoutLog.CheckOutTime.Value - workoutLog.CheckInTime).TotalMinutes;

        await _repository.UpdateAsync(workoutLog);
        
        _logger.LogInformation("User {UserId} checked out from workout {WorkoutLogId}. Duration: {Duration} minutes", 
            userId, workoutLogId, workoutLog.Duration);

        // Publish WorkoutLoggedEvent
        try
        {
            var workoutEvent = new WorkoutLoggedEvent
            {
                WorkoutId = workoutLog.Id,
                UserId = (int)workoutLog.UserId.GetHashCode(), // Convert Guid to int for event
                WorkoutName = $"Gym Session at Gym #{workoutLog.GymId}",
                WorkoutDate = workoutLog.CheckOutTime.Value,
                DurationMinutes = workoutLog.Duration.Value,
                TotalExercises = 0 // Not tracked in this simple check-in/out system
            };
            await _eventPublisher.PublishAsync(workoutEvent);
            _logger.LogInformation("Published WorkoutLoggedEvent for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish WorkoutLoggedEvent for user {UserId}", userId);
            // Don't throw - event publishing failure shouldn't fail the checkout
        }

        return MapToDTO(workoutLog);
    }

    public async Task<WorkoutStatsDTO> GetWorkoutStatsAsync(Guid userId)
    {
        var totalWorkouts = await _repository.GetTotalWorkoutsAsync(userId);
        var totalMinutes = await _repository.GetTotalMinutesAsync(userId);
        var lastWorkout = await _repository.GetLastWorkoutAsync(userId);
        var workoutsThisWeek = await _repository.GetWorkoutsThisWeekAsync(userId);
        var workoutsThisMonth = await _repository.GetWorkoutsThisMonthAsync(userId);

        return new WorkoutStatsDTO
        {
            TotalWorkouts = totalWorkouts,
            TotalMinutes = totalMinutes,
            AverageDuration = totalWorkouts > 0 ? (double)totalMinutes / totalWorkouts : 0,
            LastWorkout = lastWorkout,
            WorkoutsThisWeek = workoutsThisWeek,
            WorkoutsThisMonth = workoutsThisMonth
        };
    }

    private static WorkoutLogDTO MapToDTO(WorkoutLog log) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        GymId = log.GymId,
        CheckInTime = log.CheckInTime,
        CheckOutTime = log.CheckOutTime,
        Duration = log.Duration,
        CreatedAt = log.CreatedAt
    };
}
