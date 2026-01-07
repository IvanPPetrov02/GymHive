using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using MembershipService.BLL.RepositoryInterfaces;

namespace MembershipService.Services;

public class MembershipEventConsumer : IHostedService
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MembershipEventConsumer> _logger;

    public MembershipEventConsumer(
        IEventSubscriber eventSubscriber,
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        ILogger<MembershipEventConsumer> logger)
    {
        _eventSubscriber = eventSubscriber;
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Membership Event Consumer");

        // Subscribe to SAGA events
        _eventSubscriber.Subscribe<UserDeletedEvent>(HandleUserDeletedAsync);
        _eventSubscriber.Subscribe<GymDeletedEvent>(HandleGymDeletedAsync);
        
        _eventSubscriber.StartConsuming();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Membership Event Consumer");
        _eventSubscriber.StopConsuming();
        return Task.CompletedTask;
    }

    /// <summary>
    /// User Deletion SAGA - Delete all memberships for deleted user
    /// </summary>
    private async Task HandleUserDeletedAsync(UserDeletedEvent @event)
    {
        _logger.LogInformation("========== SAGA: UserDeleted - Deleting Memberships ==========");
        _logger.LogInformation("SagaId: {SagaId}", @event.SagaId);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMembershipRepository>();

            // Get all memberships for this user
            var memberships = await repository.GetByUserIdAsync(@event.UserId);
            
            if (!memberships.Any())
            {
                _logger.LogInformation("No memberships found for user {UserId}", @event.UserId);
            }
            else
            {
                // Delete all memberships
                foreach (var membership in memberships)
                {
                    if (membership.Id != null)
                    {
                        await repository.DeleteAsync(membership.Id);
                        _logger.LogInformation("Deleted membership {MembershipId} for user {UserId} at gym {GymId}", 
                            membership.Id, @event.UserId, membership.GymId);
                    }
                }
                
                _logger.LogInformation("✅ Deleted {Count} membership(s) for user {UserId}", 
                    memberships.Count(), @event.UserId);
            }

            // Publish completion event for SAGA coordination
            await _eventPublisher.PublishAsync(new UserMembershipsDeletedEvent
            {
                UserId = @event.UserId,
                SagaId = @event.SagaId,
                DeletedCount = memberships.Count()
            });

            _logger.LogInformation("✅ Published UserMembershipsDeletedEvent for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR in User Deletion SAGA - Failed to delete memberships for user {UserId}", @event.UserId);
            
            // In production: Consider publishing compensation event or moving to DLQ
            // For now, log the error and let RabbitMQ retry
            throw;
        }
    }

    /// <summary>
    /// Gym Deletion SAGA - Delete all memberships for deleted gym
    /// </summary>
    private async Task HandleGymDeletedAsync(GymDeletedEvent @event)
    {
        _logger.LogInformation("========== SAGA: GymDeleted - Deleting Memberships ==========");
        _logger.LogInformation("GymId: {GymId}, GymName: {GymName}", @event.GymId, @event.GymName);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IMembershipRepository>();

            // Get all memberships for this gym
            var memberships = await repository.GetByGymIdAsync(@event.GymId);
            
            if (!memberships.Any())
            {
                _logger.LogInformation("No memberships found for gym {GymId}", @event.GymId);
            }
            else
            {
                // Delete all memberships
                foreach (var membership in memberships)
                {
                    if (membership.Id != null)
                    {
                        await repository.DeleteAsync(membership.Id);
                        _logger.LogInformation("Deleted membership {MembershipId} for user {UserId} at gym {GymId}", 
                            membership.Id, membership.UserId, @event.GymId);
                    }
                }
                
                _logger.LogInformation("✅ Deleted {Count} membership(s) for gym {GymId}", 
                    memberships.Count(), @event.GymId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR in Gym Deletion SAGA - Failed to delete memberships for gym {GymId}", @event.GymId);
            
            // In production: Consider publishing compensation event or moving to DLQ
            // For now, log the error and let RabbitMQ retry
            throw;
        }
    }
}
