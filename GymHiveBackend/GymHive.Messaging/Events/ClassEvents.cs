namespace GymHive.Messaging.Events;

public class ClassCreatedEvent : BaseEvent
{
    public override string EventType => "ClassCreated";
    
    public int ClassId { get; set; }
    public int GymId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public int MaxCapacity { get; set; }
}

public class ClassBookedEvent : BaseEvent
{
    public override string EventType => "ClassBooked";
    
    public int BookingId { get; set; }
    public int ClassId { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public DateTime BookedAt { get; set; }
}

public class ClassCancelledEvent : BaseEvent
{
    public override string EventType => "ClassCancelled";
    
    public int ClassId { get; set; }
    public int GymId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class BookingCancelledEvent : BaseEvent
{
    public override string EventType => "BookingCancelled";
    
    public int BookingId { get; set; }
    public int ClassId { get; set; }
    public Guid UserId { get; set; }
}
