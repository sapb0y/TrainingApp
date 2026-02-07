using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IWeightTrackingService
{
    Task<WeightLog> LogWeightAsync(Guid userId, DateOnly date, decimal weightKg, string? notes, CancellationToken ct = default);
    Task<List<WeightLog>> GetWeightHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<WeightLog?> GetLatestWeightAsync(Guid userId, CancellationToken ct = default);
}
