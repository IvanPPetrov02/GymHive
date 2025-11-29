namespace WorkoutLoggingService.DAL.Entities;

public class WorkoutLog
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public int? Duration { get; set; } // Duration in minutes
    public DateTime CreatedAt { get; set; }
}
