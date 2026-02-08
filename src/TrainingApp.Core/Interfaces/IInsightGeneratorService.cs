using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IInsightGeneratorService
{
    Task<List<Insight>> GenerateInsightsAsync(Guid userId, CancellationToken ct);
    Task<List<Insight>> GetInsightHistoryAsync(Guid userId, DateOnly from, DateOnly to, string? category, CancellationToken ct);
}
