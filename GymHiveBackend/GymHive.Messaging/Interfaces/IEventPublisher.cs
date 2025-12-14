using GymHive.Messaging.Events;

namespace GymHive.Messaging.Interfaces;

/// <summary>
/// Interface for publishing events to the message bus
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publish an event to the specified exchange
    /// </summary>
    /// <typeparam name="TEvent">Type of event to publish</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="exchangeName">Exchange name (defaults to event type)</param>
    Task PublishAsync<TEvent>(TEvent @event, string? exchangeName = null) where TEvent : IEvent;
}
