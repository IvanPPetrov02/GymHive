namespace GymHive.Messaging.Events;

/// <summary>
/// Base class for all events in the system
/// </summary>
public abstract class BaseEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}
