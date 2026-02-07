using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IDeficitPhaseService
{
    Task<DeficitPhase> StartDeficitAsync(Guid userId, decimal startWeightKg, decimal? targetWeightKg, decimal weeklyRateKg, DeficitStrategy strategy, int? dietBreakIntervalWeeks, string? notes, CancellationToken ct = default);
    Task<DeficitPhase?> GetActiveDeficitAsync(Guid userId, CancellationToken ct = default);
    Task<DeficitPhase> EndDeficitAsync(Guid userId, CancellationToken ct = default);
    Task<DeficitPhase> PauseDeficitAsync(Guid userId, CancellationToken ct = default);
    Task<DeficitPhase> ResumeDeficitAsync(Guid userId, CancellationToken ct = default);
    Task<List<DeficitPhase>> GetDeficitHistoryAsync(Guid userId, CancellationToken ct = default);
}
