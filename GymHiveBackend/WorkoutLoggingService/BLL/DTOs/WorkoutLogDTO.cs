namespace WorkoutLoggingService.BLL.DTOs;

public record WorkoutLogDTO
{
    public int Id { get; init; }
    public Guid UserId { get; init; }
    public int GymId { get; init; }
    public DateTime CheckInTime { get; init; }
    public DateTime? CheckOutTime { get; init; }
    public int? Duration { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CheckInRequest
{
    public int GymId { get; init; }
}

public record CheckOutRequest
{
    public int WorkoutLogId { get; init; }
}

public record WorkoutStatsDTO
{
    public int TotalWorkouts { get; init; }
    public int TotalMinutes { get; init; }
    public double AverageDuration { get; init; }
    public DateTime? LastWorkout { get; init; }
    public int WorkoutsThisWeek { get; init; }
    public int WorkoutsThisMonth { get; init; }
}
