namespace GymHive.Messaging.Events;

public class UserRegisteredEvent : BaseEvent
{
    public override string EventType => "UserRegistered";
    
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int RoleId { get; set; }
}

public class UserLoggedInEvent : BaseEvent
{
    public override string EventType => "UserLoggedIn";
    
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
}

public class UserRoleChangedEvent : BaseEvent
{
    public override string EventType => "UserRoleChanged";
    
    public int UserId { get; set; }
    public int OldRoleId { get; set; }
    public int NewRoleId { get; set; }
}
