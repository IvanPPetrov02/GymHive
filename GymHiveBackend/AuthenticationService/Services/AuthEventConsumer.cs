using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using BLL.Entities;
using BLL.ManagerInterfaces;
using BLL.Encryption;

namespace AuthenticationService.Services;

public class AuthEventConsumer : IHostedService
{
    private readonly IEventSubscriber _eventSubscriber;
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthEventConsumer> _logger;

    public AuthEventConsumer(
        IEventSubscriber eventSubscriber,
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        ILogger<AuthEventConsumer> logger)
    {
        _eventSubscriber = eventSubscriber;
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Auth Event Consumer");

        _eventSubscriber.Subscribe<CreateModeratorsCommand>(HandleCreateModeratorsAsync);
        
        _eventSubscriber.StartConsuming();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Auth Event Consumer");
        _eventSubscriber.StopConsuming();
        return Task.CompletedTask;
    }

    private async Task HandleCreateModeratorsAsync(CreateModeratorsCommand @event)
    {
        _logger.LogInformation("========== Creating Moderators for Gym ==========");
        _logger.LogInformation("GymId: {GymId}, GymName: {GymName}, Count: {Count}", 
            @event.GymId, @event.GymName, @event.Moderators.Count);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<IUserManager>();

            var createdModerators = new List<CreatedModeratorInfo>();
            var createdCount = 0;
            var failedCount = 0;
            var gymNameSlug = @event.GymName.ToLower().Replace(" ", "");
            var defaultPassword = "Moderator123!"; // Default password from frontend

            foreach (var moderator in @event.Moderators)
            {
                // Generate email: firstname.lastname@gymname.com
                var baseEmail = $"{moderator.FirstName.ToLower()}.{moderator.LastName.ToLower()}@{gymNameSlug}.com";
                var email = baseEmail;
                var counter = 1;

                // Check if email exists and increment with 02, 03, etc.
                var existingUser = await userManager.GetUserByEmailAsync(email);
                while (existingUser != null)
                {
                    counter++;
                    email = $"{moderator.FirstName.ToLower()}.{moderator.LastName.ToLower()}{counter:D2}@{gymNameSlug}.com";
                    existingUser = await userManager.GetUserByEmailAsync(email);
                }

                // Register the user using the existing register function
                await userManager.RegisterUserAsync(new BLL.DTOs.UserRegisterDTO
                {
                    Email = email,
                    Password = defaultPassword,
                    Name = moderator.FirstName,
                    Surname = moderator.LastName
                });

                // Immediately update role to Moderator
                var createdUser = await userManager.GetUserByEmailAsync(email);
                if (createdUser != null)
                {
                    await userManager.UpdateUserRoleAsync(createdUser.UUID.ToString(), Role.Moderator);

                    createdModerators.Add(new CreatedModeratorInfo
                    {
                        UserId = createdUser.UUID,
                        Email = email,
                        FirstName = moderator.FirstName,
                        LastName = moderator.LastName
                    });

                    createdCount++;
                }
                else
                {
                    failedCount++;
                    _logger.LogError("Failed to retrieve created moderator user record");
                }
            }

            // Publish ModeratorsCreatedEvent so GymService can link moderators to gym
            await _eventPublisher.PublishAsync(new ModeratorsCreatedEvent
            {
                GymId = @event.GymId,
                Moderators = createdModerators
            });

            _logger.LogInformation("✅ Created {Count} moderator(s) for gym {GymId} (Failed: {Failed})",
                createdCount, @event.GymId, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR creating moderators for gym {GymId}", @event.GymId);
            throw;
        }
    }
}
