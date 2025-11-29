using System.Text;
using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace GymHive.Messaging.RabbitMQ;

public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQEventPublisher> _logger;

    public RabbitMQEventPublisher(string connectionString, ILogger<RabbitMQEventPublisher> logger)
    {
        _logger = logger;
        
        var factory = new ConnectionFactory 
        { 
            Uri = new Uri(connectionString)
        };
        
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        
        _logger.LogInformation("RabbitMQ EventPublisher connected");
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string? exchangeName = null) where TEvent : IEvent
    {
        try
        {
            exchangeName ??= @event.EventType;
            
            // Declare exchange
            await _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false
            );

            // Serialize event
            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);

            // Publish message
            await _channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: string.Empty,
                body: body
            );

            _logger.LogInformation(
                "Published event {EventType} with ID {EventId} to exchange {ExchangeName}",
                @event.EventType,
                @event.EventId,
                exchangeName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType}", @event.EventType);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
