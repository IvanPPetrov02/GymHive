using System.Text;
using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GymHive.Messaging.RabbitMQ;

public class RabbitMQEventSubscriber : IEventSubscriber, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQEventSubscriber> _logger;
    private readonly string _serviceName;
    private readonly List<(AsyncEventingBasicConsumer Consumer, string QueueName)> _consumers = new();

    public RabbitMQEventSubscriber(
        string connectionString, 
        string serviceName, 
        ILogger<RabbitMQEventSubscriber> logger)
    {
        _serviceName = serviceName;
        _logger = logger;
        
        var factory = new ConnectionFactory 
        { 
            Uri = new Uri(connectionString)
        };
        
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        
        _logger.LogInformation("RabbitMQ EventSubscriber connected for service {ServiceName}", _serviceName);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler, string? queueName = null) where TEvent : IEvent
    {
        var eventType = typeof(TEvent).Name.Replace("Event", "");
        var exchangeName = eventType;
        queueName ??= $"{_serviceName}.{eventType}";

        try
        {
            // Declare exchange
            _channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false
            ).GetAwaiter().GetResult();

            // Declare queue
            _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            ).GetAwaiter().GetResult();

            // Bind queue to exchange
            _channel.QueueBindAsync(
                queue: queueName,
                exchange: exchangeName,
                routingKey: string.Empty
            ).GetAwaiter().GetResult();

            // Create consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var @event = JsonConvert.DeserializeObject<TEvent>(message);

                    if (@event != null)
                    {
                        _logger.LogInformation(
                            "Received event {EventType} with ID {EventId}",
                            @event.EventType,
                            @event.EventId
                        );

                        await handler(@event);

                        // Acknowledge message
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event from queue {QueueName}", queueName);
                    
                    // Negative acknowledge - requeue message
                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag, 
                        multiple: false, 
                        requeue: true
                    );
                }
            };

            _consumers.Add((consumer, queueName));

            _logger.LogInformation(
                "Subscribed to {EventType} on queue {QueueName}",
                eventType,
                queueName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to event {EventType}", eventType);
            throw;
        }
    }

    public void StartConsuming()
    {
        foreach (var (consumer, queueName) in _consumers)
        {
            _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer
            ).GetAwaiter().GetResult();
            
            _logger.LogInformation("Started consuming from queue {QueueName}", queueName);
        }

        _logger.LogInformation("Started consuming messages for service {ServiceName}", _serviceName);
    }

    public void StopConsuming()
    {
        _logger.LogInformation("Stopping message consumption for service {ServiceName}", _serviceName);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
