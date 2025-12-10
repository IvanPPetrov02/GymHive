namespace GymHive.Messaging.Events;

public class MembershipPurchasedEvent : BaseEvent
{
    public override string EventType => "MembershipPurchased";
    
    public string? MembershipId { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
}

public class MembershipCancelledEvent : BaseEvent
{
    public override string EventType => "MembershipCancelled";
    
    public string? MembershipId { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class MembershipExpiredEvent : BaseEvent
{
    public override string EventType => "MembershipExpired";
    
    public string? MembershipId { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
}

public class MembershipExpiringEvent : BaseEvent
{
    public override string EventType => "MembershipExpiring";
    
    public string? MembershipId { get; set; }
    public Guid UserId { get; set; }
    public int GymId { get; set; }
    public string GymName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
}
