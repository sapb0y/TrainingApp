using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Orchestration.Services;

public class DailyMetricsJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyMetricsJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public DailyMetricsJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyMetricsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyMetricsJob started. Will run every {Interval} hour(s)", _interval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRecentWorkoutsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing daily metrics");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessRecentWorkoutsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var fatigueService = scope.ServiceProvider.GetRequiredService<IFatigueModelService>();

        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);

        var usersWithRecentWorkouts = await db.Workouts
            .AsNoTracking()
            .Where(w => w.Status == WorkoutStatus.Completed
                && w.CompletedAt.HasValue
                && w.CompletedAt > cutoff)
            .Select(w => new { w.UserId, Date = w.CompletedAt!.Value })
            .Distinct()
            .ToListAsync(ct);

        foreach (var entry in usersWithRecentWorkouts)
        {
            try
            {
                var date = DateOnly.FromDateTime(entry.Date.UtcDateTime);
                await fatigueService.UpdateDailyMetricsAsync(entry.UserId, date, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update metrics for user {UserId}", entry.UserId);
            }
        }

        if (usersWithRecentWorkouts.Count > 0)
            _logger.LogInformation("DailyMetricsJob processed {Count} user-date pairs", usersWithRecentWorkouts.Count);
    }
}
