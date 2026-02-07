using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IFatigueModelService
{
    Task UpdateDailyMetricsAsync(Guid userId, DateOnly date, CancellationToken ct = default);
    Task<DailyMetrics?> GetReadinessAsync(Guid userId, CancellationToken ct = default);
    Task<List<DailyMetrics>> GetHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<RecoveryLog> LogRecoveryAsync(Guid userId, DateOnly date, int? sleepQuality, decimal? sleepHours,
        int? stressLevel, int? energyLevel, int? muscleReadiness, int? mood, string? notes, CancellationToken ct = default);
    Task<List<RecoveryLog>> GetRecoveryLogsAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task RecalculateAsync(Guid userId, DateOnly? from, CancellationToken ct = default);
}
