using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using NotificationsService.BLL.Entities;
using NotificationsService.BLL.RepositoryInterfaces;

namespace NotificationsService.Services;

public class NotificationEventConsumer : IHostedService
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationEventConsumer> _logger;

    public NotificationEventConsumer(
        IEventSubscriber eventSubscriber,
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        ILogger<NotificationEventConsumer> logger)
    {
        _eventSubscriber = eventSubscriber;
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Notification Event Consumer");

        // Subscribe to SAGA events
        _eventSubscriber.Subscribe<UserDeletedEvent>(HandleUserDeletedAsync);
        
        // Subscribe to all event types
        _eventSubscriber.Subscribe<UserRegisteredEvent>(HandleUserRegisteredAsync);
        _eventSubscriber.Subscribe<MembershipPurchasedEvent>(HandleMembershipPurchasedAsync);
        _eventSubscriber.Subscribe<MembershipExpiringEvent>(HandleMembershipExpiringAsync);
        _eventSubscriber.Subscribe<GymGroupMemberAddedEvent>(HandleGymGroupMemberAddedAsync);
        _eventSubscriber.Subscribe<ClassCreatedEvent>(HandleClassCreatedAsync);
        _eventSubscriber.Subscribe<ClassBookedEvent>(HandleClassBookedAsync);
        _eventSubscriber.Subscribe<WorkoutLoggedEvent>(HandleWorkoutLoggedAsync);
        _eventSubscriber.Subscribe<PostCreatedEvent>(HandlePostCreatedAsync);
        _eventSubscriber.Subscribe<CommentAddedEvent>(HandleCommentAddedAsync);
        _eventSubscriber.Subscribe<PostLikedEvent>(HandlePostLikedAsync);
        
        _eventSubscriber.StartConsuming();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Notification Event Consumer");
        _eventSubscriber.StopConsuming();
        return Task.CompletedTask;
    }

    private async Task HandleUserRegisteredAsync(UserRegisteredEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var notification = new Notification
            {
                UserId = @event.UserId,
                Type = "Welcome",
                Title = "Welcome to GymHive!",
                Message = $"Welcome {@event.Username}! Start exploring gyms and book your first class.",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created welcome notification for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling UserRegisteredEvent");
        }
    }

    private async Task HandleMembershipPurchasedAsync(MembershipPurchasedEvent @event)
    {
        _logger.LogInformation("========== RECEIVED MembershipPurchasedEvent ==========");
        _logger.LogInformation("UserId: {UserId}, GymId: {GymId}, EndDate: {EndDate}", @event.UserId, @event.GymId, @event.EndDate);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Welcome notification for new membership with detailed instructions
            var notification = new Notification
            {
                UserId = @event.UserId,
                Type = "MembershipPurchased",
                Title = $"🎉 Welcome to {@event.GymName}!",
                Message = $"Congratulations on joining {@event.GymName}! Your membership is now active and valid until {@event.EndDate:MMM dd, yyyy}.\n\n" +
                          $"📱 **How to Check-In at {@event.GymName}:**\n" +
                          $"1. Go to the Feed page in the app\n" +
                          $"2. Click the 'Show QR Code' button at the top\n" +
                          $"3. Show your QR code to the gym staff to log your visit\n\n" +
                          $"💪 Your QR code refreshes every 30 seconds for security.\n" +
                          $"Start your fitness journey at {@event.GymName} today!",
                RelatedEntityId = @event.GymId.ToString(),
                RelatedEntityType = "gym",
                IsRead = false
            };

            _logger.LogInformation("About to save notification to database...");
            var savedNotification = await repository.CreateAsync(notification);
            _logger.LogInformation("✅ Successfully created notification with ID {NotificationId} for user {UserId}", savedNotification.Id, @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR handling MembershipPurchasedEvent - UserId: {UserId}", @event.UserId);
        }
    }

    private async Task HandleMembershipExpiringAsync(MembershipExpiringEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            string title;
            string message;

            if (@event.DaysRemaining == 1)
            {
                title = "⚠️ Membership Expires Tomorrow!";
                message = $"Your membership at {@event.GymName} expires tomorrow ({@event.EndDate:MMM dd, yyyy}). Renew now to continue your fitness journey without interruption!";
            }
            else if (@event.DaysRemaining == 3)
            {
                title = "⏰ Membership Expiring Soon";
                message = $"Your membership at {@event.GymName} expires in {@event.DaysRemaining} days ({@event.EndDate:MMM dd, yyyy}). Don't forget to renew!";
            }
            else
            {
                title = "📅 Membership Reminder";
                message = $"Just a heads up! Your membership at {@event.GymName} expires in {@event.DaysRemaining} days ({@event.EndDate:MMM dd, yyyy}). Consider renewing to keep your access.";
            }

            var notification = new Notification
            {
                UserId = @event.UserId,
                Type = "MembershipExpiring",
                Title = title,
                Message = message,
                RelatedEntityId = @event.GymId.ToString(),
                RelatedEntityType = "gym",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created membership expiring notification for user {UserId}, {Days} days remaining", 
                @event.UserId, @event.DaysRemaining);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MembershipExpiringEvent");
        }
    }

    private async Task HandleClassCreatedAsync(ClassCreatedEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Notify all gym members about new class (implement logic to get gym members)
            _logger.LogInformation("New class created: {ClassName} at gym {GymId}", @event.ClassName, @event.GymId);
            
            // TODO: Get all members of this gym and create notifications for them
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ClassCreatedEvent");
        }
    }

    private async Task HandleClassBookedAsync(ClassBookedEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var notification = new Notification
            {
                UserId = @event.UserId,
                Type = "ClassBooked",
                Title = "Class Booking Confirmed",
                Message = $"Your booking for class has been confirmed for {@event.BookedAt:MMM dd, yyyy}.",
                RelatedEntityId = @event.ClassId.ToString(),
                RelatedEntityType = "class",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created class booking notification for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ClassBookedEvent");
        }
    }

    private async Task HandleWorkoutLoggedAsync(WorkoutLoggedEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var notification = new Notification
            {
                UserId = @event.UserId,
                Type = "WorkoutLogged",
                Title = "Workout Logged!",
                Message = $"Great job! You completed {@event.WorkoutName} ({@event.DurationMinutes} minutes, {@event.TotalExercises} exercises).",
                RelatedEntityId = @event.WorkoutId.ToString(),
                RelatedEntityType = "workout",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created workout logged notification for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WorkoutLoggedEvent");
        }
    }

    private async Task HandlePostCreatedAsync(PostCreatedEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Notify followers/friends about new post
            _logger.LogInformation("New post created by user {UserId}", @event.UserId);
            
            // TODO: Get user's friends/followers and create notifications
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PostCreatedEvent");
        }
    }

    private async Task HandleCommentAddedAsync(CommentAddedEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Notify post author about new comment
            var notification = new Notification
            {
                UserId = Guid.Parse(@event.PostAuthorId.ToString()),
                Type = "NewComment",
                Title = "New Comment on Your Post",
                Message = "Someone commented on your post.",
                RelatedEntityId = @event.PostId.ToString(),
                RelatedEntityType = "post",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created comment notification for post author {PostAuthorId}", @event.PostAuthorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling CommentAddedEvent");
        }
    }

    private async Task HandlePostLikedAsync(PostLikedEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Notify post author about like
            var notification = new Notification
            {
                UserId = Guid.Parse(@event.PostAuthorId.ToString()),
                Type = "PostLiked",
                Title = "Someone Liked Your Post",
                Message = "Your post received a new like!",
                RelatedEntityId = @event.PostId.ToString(),
                RelatedEntityType = "post",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created like notification for post author {PostAuthorId}", @event.PostAuthorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PostLikedEvent");
        }
    }

    private async Task HandleGymGroupMemberAddedAsync(GymGroupMemberAddedEvent @event)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Notify user about being added to gym group
            var notification = new Notification
            {
                UserId = @event.UserId,
                Type = "GymGroupMemberAdded",
                Title = "📋 Added to Gym Group!",
                Message = $"You've been added to the '{@event.GymGroupName}' group at {@event.GymName}!\n\n" +
                          $"🏋️ **What's Next:**\n" +
                          $"• Check the group schedule for upcoming classes\n" +
                          $"• Connect with other members in your group\n" +
                          $"• Participate in group challenges and activities\n\n" +
                          $"Welcome to the community!",
                RelatedEntityId = @event.GymGroupId.ToString(),
                RelatedEntityType = "gymgroup",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created gym group member added notification for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GymGroupMemberAddedEvent");
        }
    }

    /// <summary>
    /// User Deletion SAGA - Delete all notifications for deleted user
    /// </summary>
    private async Task HandleUserDeletedAsync(UserDeletedEvent @event)
    {
        _logger.LogInformation("========== SAGA: UserDeleted - Deleting Notifications ==========");
        _logger.LogInformation("SagaId: {SagaId}", @event.SagaId);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            // Get all notifications for this user
            var notifications = await repository.GetByUserIdAsync(@event.UserId);
            
            if (!notifications.Any())
            {
                _logger.LogInformation("No notifications found for user {UserId}", @event.UserId);
            }
            else
            {
                // Delete all notifications
                foreach (var notification in notifications)
                {
                    await repository.DeleteAsync(notification.Id);
                    _logger.LogInformation("Deleted notification {NotificationId} for user {UserId}", 
                        notification.Id, @event.UserId);
                }
                
                _logger.LogInformation("✅ Deleted {Count} notification(s) for user {UserId}", 
                    notifications.Count(), @event.UserId);
            }

            // Publish completion event for SAGA coordination
            await _eventPublisher.PublishAsync(new UserNotificationsDeletedEvent
            {
                UserId = @event.UserId,
                SagaId = @event.SagaId,
                DeletedCount = notifications.Count()
            });

            _logger.LogInformation("✅ Published UserNotificationsDeletedEvent for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR in User Deletion SAGA - Failed to delete notifications for user {UserId}", @event.UserId);
            
            // In production: Consider publishing compensation event or moving to DLQ
            // For now, log the error and let RabbitMQ retry
            throw;
        }
    }
}
