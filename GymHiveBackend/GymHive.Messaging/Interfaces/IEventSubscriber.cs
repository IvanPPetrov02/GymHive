using GymHive.Messaging.Events;

namespace GymHive.Messaging.Interfaces;

/// <summary>
/// Interface for subscribing to events from the message bus
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    /// <typeparam name="TEvent">Type of event to subscribe to</typeparam>
    /// <param name="handler">Handler function to process the event</param>
    /// <param name="queueName">Queue name (defaults to event type + service name)</param>
    void Subscribe<TEvent>(Func<TEvent, Task> handler, string? queueName = null) where TEvent : IEvent;
    
    /// <summary>
    /// Start consuming messages from all subscribed queues
    /// </summary>
    void StartConsuming();
    
    /// <summary>
    /// Stop consuming messages
    /// </summary>
    void StopConsuming();
}
