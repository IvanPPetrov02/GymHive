namespace GymHive.Messaging.Events;

public class MembershipPurchasedEvent : BaseEvent
{
    public override string EventType => "MembershipPurchased";
    
    public int MembershipId { get; set; }
    public int UserId { get; set; }
    public int GymId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
}

public class MembershipCancelledEvent : BaseEvent
{
    public override string EventType => "MembershipCancelled";
    
    public int MembershipId { get; set; }
    public int UserId { get; set; }
    public int GymId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class MembershipExpiredEvent : BaseEvent
{
    public override string EventType => "MembershipExpired";
    
    public int MembershipId { get; set; }
    public int UserId { get; set; }
    public int GymId { get; set; }
}
