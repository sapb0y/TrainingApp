using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Orchestration.Services;

public class WorkoutReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkoutReminderService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public WorkoutReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkoutReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WorkoutReminderService started. Checking every {Interval} minutes", _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckUpcomingWorkoutsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking upcoming workouts");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckUpcomingWorkoutsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var reminderWindow = now.AddMinutes(30);

        var upcomingWorkouts = await db.Workouts
            .AsNoTracking()
            .Where(w => w.Status == WorkoutStatus.Planned)
            .Where(w => w.ScheduledAt >= now && w.ScheduledAt <= reminderWindow)
            .Select(w => new { w.Id, w.Name, w.ScheduledAt, w.UserId })
            .ToListAsync(ct);

        foreach (var workout in upcomingWorkouts)
        {
            // TODO: Implement notification system (push, email, etc.)
            _logger.LogInformation(
                "Upcoming workout reminder: {WorkoutName} scheduled at {ScheduledAt} for user {UserId}",
                workout.Name,
                workout.ScheduledAt,
                workout.UserId);
        }

        if (upcomingWorkouts.Count > 0)
        {
            _logger.LogInformation("Found {Count} upcoming workouts in reminder window", upcomingWorkouts.Count);
        }
    }
}
