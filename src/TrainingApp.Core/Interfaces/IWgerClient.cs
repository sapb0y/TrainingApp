using TrainingApp.Core.DTOs;

namespace TrainingApp.Core.Interfaces;

public interface IWgerClient
{
    Task<WgerExerciseResponse> GetExercisesAsync(int page = 1, string? language = "en", CancellationToken ct = default);
    Task<WgerExerciseDto?> GetExerciseAsync(int id, CancellationToken ct = default);
    Task<WgerCategoryResponse> GetCategoriesAsync(CancellationToken ct = default);
    Task<WgerMuscleResponse> GetMusclesAsync(CancellationToken ct = default);
    Task<WgerEquipmentResponse> GetEquipmentAsync(CancellationToken ct = default);
}
