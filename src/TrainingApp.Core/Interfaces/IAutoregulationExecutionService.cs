using TrainingApp.Core.Entities;
using TrainingApp.Core.Services;

namespace TrainingApp.Core.Interfaces;

public interface IAutoregulationExecutionService
{
    Task<AutoregulationService.WorkoutEvaluation> GetRecommendationsAsync(Guid workoutId, CancellationToken ct = default);
    Task<List<AdaptationLog>> GetAdaptationLogAsync(Guid workoutId, CancellationToken ct = default);
}
