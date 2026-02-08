using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface ICardioTrackingService
{
    Task<CardioSession> LogCardioAsync(Guid userId, DateOnly date, CardioModality modality,
        CardioIntensityZone zone, int durationMinutes, decimal? distanceKm,
        int? avgHr, int? maxHr, DateTimeOffset? startedAt, string? notes, CancellationToken ct = default);
    Task<CardioSession?> GetCardioSessionAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<List<CardioSession>> GetCardioHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task DeleteCardioAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<List<CardioSession>> GetCardioForDateAsync(Guid userId, DateOnly date, CancellationToken ct = default);
}
