using GymHive.Messaging.Interfaces;
using GymHive.Messaging.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace GymHive.Messaging;

public static class MessagingExtensions
{
    /// <summary>
    /// Add RabbitMQ event bus to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">RabbitMQ connection string</param>
    /// <param name="serviceName">Name of the service (for queue naming)</param>
    public static IServiceCollection AddRabbitMQEventBus(
        this IServiceCollection services, 
        string connectionString, 
        string serviceName)
    {
        services.AddSingleton<IEventPublisher>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMQEventPublisher>>();
            return new RabbitMQEventPublisher(connectionString, logger);
        });

        services.AddSingleton<IEventSubscriber>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMQEventSubscriber>>();
            return new RabbitMQEventSubscriber(connectionString, serviceName, logger);
        });

        return services;
    }
}
