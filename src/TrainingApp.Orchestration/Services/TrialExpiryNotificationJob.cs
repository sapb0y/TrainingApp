using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Orchestration.Services;

public class TrialExpiryNotificationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrialExpiryNotificationJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public TrialExpiryNotificationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<TrialExpiryNotificationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TrialExpiryNotificationJob started. Runs every {Interval}h", _interval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotifications(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in TrialExpiryNotificationJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessNotifications(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTimeOffset.UtcNow;

        // 7-day warning: TrialEndDate between 6 and 7 days from now
        var sevenDayStart = now.AddDays(6);
        var sevenDayEnd = now.AddDays(7);

        var sevenDaySubs = await db.UserSubscriptions
            .Include(s => s.User)
            .Where(s => s.Status == SubscriptionStatus.Trial
                && s.TrialEndDate.HasValue
                && s.TrialEndDate.Value >= sevenDayStart
                && s.TrialEndDate.Value < sevenDayEnd)
            .ToListAsync(ct);

        foreach (var sub in sevenDaySubs)
        {
            if (sub.User?.Email is not null)
            {
                await emailService.SendTrialExpiringAsync(sub.User.Email, sub.User.DisplayName, 7, ct);
                _logger.LogInformation("Sent 7-day trial warning to {Email}", sub.User.Email);
            }
        }

        // Final warning: TrialEndDate between 0 and 1 day from now
        var oneDayEnd = now.AddDays(1);

        var finalSubs = await db.UserSubscriptions
            .Include(s => s.User)
            .Where(s => s.Status == SubscriptionStatus.Trial
                && s.TrialEndDate.HasValue
                && s.TrialEndDate.Value >= now
                && s.TrialEndDate.Value < oneDayEnd)
            .ToListAsync(ct);

        foreach (var sub in finalSubs)
        {
            if (sub.User?.Email is not null)
            {
                await emailService.SendTrialExpiringAsync(sub.User.Email, sub.User.DisplayName, 1, ct);
                _logger.LogInformation("Sent final trial warning to {Email}", sub.User.Email);
            }
        }

        _logger.LogInformation("Trial expiry check: {SevenDay} 7-day, {Final} final warnings sent",
            sevenDaySubs.Count, finalSubs.Count);
    }
}
