using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using NotificationsService.DAL.Entities;
using NotificationsService.DAL.RepositoryInterfaces;

namespace NotificationsService.Services;

public class NotificationEventConsumer : IHostedService
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationEventConsumer> _logger;

    public NotificationEventConsumer(
        IEventSubscriber eventSubscriber,
        IServiceProvider serviceProvider,
        ILogger<NotificationEventConsumer> logger)
    {
        _eventSubscriber = eventSubscriber;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Notification Event Consumer");

        // Subscribe to all event types
        _eventSubscriber.Subscribe<UserRegisteredEvent>(HandleUserRegisteredAsync);
        _eventSubscriber.Subscribe<MembershipPurchasedEvent>(HandleMembershipPurchasedAsync);
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
                UserId = Guid.Parse(@event.UserId.ToString()),
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
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

            var notification = new Notification
            {
                UserId = Guid.Parse(@event.UserId.ToString()),
                Type = "MembershipPurchased",
                Title = "Membership Activated!",
                Message = $"Your gym membership is now active until {@event.EndDate:MMM dd, yyyy}.",
                RelatedEntityId = @event.GymId.ToString(),
                RelatedEntityType = "gym",
                IsRead = false
            };

            await repository.CreateAsync(notification);
            _logger.LogInformation("Created membership notification for user {UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MembershipPurchasedEvent");
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
                UserId = Guid.Parse(@event.UserId.ToString()),
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
                UserId = Guid.Parse(@event.UserId.ToString()),
                Type = "WorkoutLogged",
                Title = "Workout Completed!",
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
}
