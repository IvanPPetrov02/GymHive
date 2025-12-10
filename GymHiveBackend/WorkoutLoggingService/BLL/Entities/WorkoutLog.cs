namespace WorkoutLoggingService.BLL.Entities;

public class WorkoutLog
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public DateTime VisitDate { get; set; }
}
