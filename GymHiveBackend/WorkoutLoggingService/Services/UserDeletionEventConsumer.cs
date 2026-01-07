using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using WorkoutLoggingService.BLL.RepositoryInterfaces;

namespace WorkoutLoggingService.Services;

public class UserDeletionEventConsumer : IHostedService
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserDeletionEventConsumer> _logger;

    public UserDeletionEventConsumer(
        IEventSubscriber eventSubscriber,
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        ILogger<UserDeletionEventConsumer> logger)
    {
        _eventSubscriber = eventSubscriber;
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting WorkoutLoggingService User Deletion Event Consumer");

        _eventSubscriber.Subscribe<UserDeletedEvent>(HandleUserDeletedAsync);
        _eventSubscriber.StartConsuming();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping WorkoutLoggingService User Deletion Event Consumer");
        _eventSubscriber.StopConsuming();
        return Task.CompletedTask;
    }

    private async Task HandleUserDeletedAsync(UserDeletedEvent @event)
    {
        _logger.LogInformation("SAGA: UserDeleted received (WorkoutLoggingService). SagaId: {SagaId}", @event.SagaId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkoutLogRepository>();

            var deletedCount = await repository.DeleteByUserIdAsync(@event.UserId);

            await _eventPublisher.PublishAsync(new UserWorkoutLogsDeletedEvent
            {
                UserId = @event.UserId,
                SagaId = @event.SagaId,
                DeletedCount = deletedCount
            });

            _logger.LogInformation(
                "SAGA: Workout logs deleted. SagaId: {SagaId}, DeletedCount: {DeletedCount}",
                @event.SagaId,
                deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAGA: Failed deleting workout logs. SagaId: {SagaId}", @event.SagaId);
            throw;
        }
    }
}
