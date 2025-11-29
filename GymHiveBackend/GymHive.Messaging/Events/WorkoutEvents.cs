namespace GymHive.Messaging.Events;

public class WorkoutLoggedEvent : BaseEvent
{
    public override string EventType => "WorkoutLogged";
    
    public int WorkoutId { get; set; }
    public int UserId { get; set; }
    public string WorkoutName { get; set; } = string.Empty;
    public DateTime WorkoutDate { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalExercises { get; set; }
}

public class WorkoutDeletedEvent : BaseEvent
{
    public override string EventType => "WorkoutDeleted";
    
    public int WorkoutId { get; set; }
    public int UserId { get; set; }
}
