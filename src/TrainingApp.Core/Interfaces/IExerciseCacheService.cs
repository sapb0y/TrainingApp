using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface IExerciseCacheService
{
    Task<IReadOnlyList<Exercise>> GetAllExercisesAsync(CancellationToken ct = default);
    Task<Exercise?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Exercise?> GetByWgerIdAsync(int wgerId, CancellationToken ct = default);
    Task<IReadOnlyList<Exercise>> SearchAsync(string query, CancellationToken ct = default);
    Task<int> SyncFromWgerAsync(CancellationToken ct = default);
}
