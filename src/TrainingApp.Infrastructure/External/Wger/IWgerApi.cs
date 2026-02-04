using Refit;
using TrainingApp.Core.DTOs;

namespace TrainingApp.Infrastructure.External.Wger;

public interface IWgerApi
{
    [Get("/exercisebaseinfo/")]
    Task<WgerExerciseResponse> GetExercisesAsync(
        [Query] int? limit = 100,
        [Query] int? offset = 0,
        [Query] string? language = "en",
        CancellationToken ct = default);

    [Get("/exercisebaseinfo/{id}/")]
    Task<WgerExerciseDto?> GetExerciseAsync(int id, CancellationToken ct = default);

    [Get("/muscle/")]
    Task<WgerMuscleResponse> GetMusclesAsync(CancellationToken ct = default);

    [Get("/equipment/")]
    Task<WgerEquipmentResponse> GetEquipmentAsync(CancellationToken ct = default);

    [Get("/exercisecategory/")]
    Task<WgerCategoryResponse> GetCategoriesAsync(CancellationToken ct = default);
}
