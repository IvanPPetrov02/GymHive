using GymHive.Messaging.Events;
using GymHive.Messaging.Interfaces;
using MembershipService.BLL.RepositoryInterfaces;

namespace MembershipService.Services;

public class MembershipExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MembershipExpirationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check once per day

    public MembershipExpirationService(
        IServiceProvider serviceProvider,
        ILogger<MembershipExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Membership Expiration Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiringMemberships(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expiring memberships");
            }

            // Wait for next check
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckExpiringMemberships(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMembershipRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var now = DateTime.UtcNow;
        var warningDays = new[] { 5, 3, 1 }; // Send notifications 5, 3, and 1 days before expiration

        foreach (var days in warningDays)
        {
            var targetDate = now.AddDays(days).Date;
            var nextDay = targetDate.AddDays(1);

            // Get memberships expiring on the target date
            var expiringMemberships = await repository.GetExpiringMembershipsAsync(targetDate, nextDay);

            foreach (var membership in expiringMemberships)
            {
                try
                {
                    await eventPublisher.PublishAsync(new MembershipExpiringEvent
                    {
                        MembershipId = membership.Id,
                        UserId = membership.UserId,
                        GymId = membership.GymId,
                        GymName = membership.GymName ?? "Your gym",
                        EndDate = membership.EndDate,
                        DaysRemaining = days
                    });

                    _logger.LogInformation(
                        "Published MembershipExpiringEvent for membership {MembershipId}, expiring in {Days} days",
                        membership.Id, days);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish MembershipExpiringEvent for membership {MembershipId}",
                        membership.Id);
                }
            }
        }

        _logger.LogInformation("Completed expiring memberships check at {Time}", DateTime.UtcNow);
    }
}
