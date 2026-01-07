using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using GymService.BLL.RepositoryInterfaces;
using GymService.BLL.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymService.Services;

public class GymEventConsumer : IHostedService
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GymEventConsumer> _logger;
    private readonly IConfiguration _configuration;

    public GymEventConsumer(
        IEventSubscriber eventSubscriber,
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        ILogger<GymEventConsumer> logger,
        IConfiguration configuration)
    {
        _eventSubscriber = eventSubscriber;
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Gym Event Consumer");

        // Subscribe to SAGA events
        _eventSubscriber.Subscribe<GymDeletedEvent>(HandleGymDeletedAsync);
        _eventSubscriber.Subscribe<ModeratorsCreatedEvent>(HandleModeratorsCreatedAsync);
        _eventSubscriber.Subscribe<UserDeletedEvent>(HandleUserDeletedAsync);
        
        _eventSubscriber.StartConsuming();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Gym Event Consumer");
        _eventSubscriber.StopConsuming();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gym Deletion SAGA - Delete gym groups and demote moderators to regular users
    /// </summary>
    private async Task HandleGymDeletedAsync(GymDeletedEvent @event)
    {
        _logger.LogInformation("========== SAGA: GymDeleted - Deleting Gym Groups & Demoting Moderators ==========");
        _logger.LogInformation("GymId: {GymId}, GymName: {GymName}", @event.GymId, @event.GymName);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var gymGroupRepository = scope.ServiceProvider.GetRequiredService<IGymGroupRepository>();

            // Get all gym groups for this gym
            var gymGroups = await gymGroupRepository.GetByGymIdAsync(@event.GymId);
            
            var moderatorIds = new List<Guid>();
            
            if (!gymGroups.Any())
            {
                _logger.LogInformation("No gym groups found for gym {GymId}", @event.GymId);
            }
            else
            {
                // Collect moderator IDs and delete gym groups
                foreach (var gymGroup in gymGroups)
                {
                    if (gymGroup.ModeratorId != Guid.Empty)
                    {
                        moderatorIds.Add(gymGroup.ModeratorId);
                    }
                    
                    await gymGroupRepository.DeleteAsync(gymGroup.Id);
                    _logger.LogInformation("Deleted gym group {GroupId} ({GroupName}) for gym {GymId}", 
                        gymGroup.Id, gymGroup.Name, @event.GymId);
                }
                
                _logger.LogInformation("✅ Deleted {Count} gym group(s) for gym {GymId}", 
                    gymGroups.Count(), @event.GymId);
            }

            // Delete gym moderator relationships from GymModerators table
            var dbContext = scope.ServiceProvider.GetRequiredService<DAL.DbContexts.GymDbContext>();
            var gymModerators = await dbContext.GymModerators
                .Where(gm => gm.GymId == @event.GymId)
                .ToListAsync();
            
            if (gymModerators.Any())
            {
                dbContext.GymModerators.RemoveRange(gymModerators);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("✅ Removed {Count} gym moderator link(s) for gym {GymId}", 
                    gymModerators.Count, @event.GymId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR in Gym Deletion SAGA - Failed to handle gym deletion for gym {GymId}", @event.GymId);
            
            // In production: Consider publishing compensation event or moving to DLQ
            // For now, log the error and let RabbitMQ retry
            throw;
        }
    }

    /// <summary>
    /// Handle moderators created event - Link moderators to gym
    /// </summary>
    private async Task HandleModeratorsCreatedAsync(ModeratorsCreatedEvent @event)
    {
        _logger.LogInformation("========== Linking Moderators to Gym ==========");
        _logger.LogInformation("EventId: {EventId}, GymId: {GymId}, Moderators: {Count}", @event.EventId, @event.GymId, @event.Moderators.Count);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DAL.DbContexts.GymDbContext>();

            var createdLinks = 0;
            var existingLinks = 0;

            foreach (var moderator in @event.Moderators)
            {
                // Check if relationship already exists
                var existingLink = await dbContext.GymModerators
                    .FirstOrDefaultAsync(gm => gm.GymId == @event.GymId && gm.ModeratorUserId == moderator.UserId);

                if (existingLink == null)
                {
                    var gymModerator = new GymModerator
                    {
                        GymId = @event.GymId,
                        ModeratorUserId = moderator.UserId,
                        AssignedAt = DateTime.UtcNow
                    };

                    await dbContext.GymModerators.AddAsync(gymModerator);
                    createdLinks++;
                }
                else
                {
                    existingLinks++;
                }
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("✅ Linked moderators to gym {GymId}. CreatedLinks: {CreatedLinks}, ExistingLinks: {ExistingLinks}",
                @event.GymId, createdLinks, existingLinks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR linking moderators to gym {GymId}", @event.GymId);
            throw;
        }
    }

    /// <summary>
    /// User Deletion cleanup - remove user from gym groups and moderator links
    /// </summary>
    private async Task HandleUserDeletedAsync(UserDeletedEvent @event)
    {
        _logger.LogInformation("UserDeleted received (GymService cleanup). SagaId: {SagaId}", @event.SagaId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var gymGroupRepository = scope.ServiceProvider.GetRequiredService<IGymGroupRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<DAL.DbContexts.GymDbContext>();

            var removedMembershipCount = await gymGroupRepository.RemoveAllMembershipsByUserIdAsync(@event.UserId);

            // Remove gym-moderator links
            var moderatorLinks = await dbContext.GymModerators
                .Where(gm => gm.ModeratorUserId == @event.UserId)
                .ToListAsync();

            if (moderatorLinks.Count > 0)
            {
                dbContext.GymModerators.RemoveRange(moderatorLinks);
                await dbContext.SaveChangesAsync();
            }

            // Detach user from any groups they moderate
            var moderatedGroups = await dbContext.GymGroups
                .Where(g => g.ModeratorId == @event.UserId)
                .ToListAsync();

            if (moderatedGroups.Count > 0)
            {
                foreach (var group in moderatedGroups)
                {
                    group.ModeratorId = Guid.Empty;
                    group.UpdatedAt = DateTime.UtcNow;
                }

                await dbContext.SaveChangesAsync();
            }

            _logger.LogInformation(
                "GymService cleanup complete. SagaId: {SagaId}, RemovedMemberships: {RemovedMemberships}, RemovedModeratorLinks: {RemovedModeratorLinks}, ModeratedGroupsUpdated: {ModeratedGroupsUpdated}",
                @event.SagaId,
                removedMembershipCount,
                moderatorLinks.Count,
                moderatedGroups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GymService cleanup failed. SagaId: {SagaId}", @event.SagaId);
            throw;
        }
    }
}
