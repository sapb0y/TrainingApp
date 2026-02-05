using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Orchestration.Services;

public class ExerciseSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExerciseSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(24);

    public ExerciseSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExerciseSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExerciseSyncService started. Will sync every {Interval} hours", _syncInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncExercisesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing exercises from wger");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }

    private async Task SyncExercisesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<IExerciseCacheService>();

        _logger.LogInformation("Starting scheduled exercise sync from wger");

        var count = await cacheService.SyncFromWgerAsync(ct);

        _logger.LogInformation("Scheduled exercise sync completed. {Count} exercises synced", count);
    }
}
