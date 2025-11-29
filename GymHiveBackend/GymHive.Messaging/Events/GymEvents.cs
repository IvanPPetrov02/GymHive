namespace GymHive.Messaging.Events;

public class GymCreatedEvent : BaseEvent
{
    public override string EventType => "GymCreated";
    
    public int GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
}

public class GymUpdatedEvent : BaseEvent
{
    public override string EventType => "GymUpdated";
    
    public int GymId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int UpdatedBy { get; set; }
}

public class GymDeletedEvent : BaseEvent
{
    public override string EventType => "GymDeleted";
    
    public int GymId { get; set; }
    public int DeletedBy { get; set; }
}
