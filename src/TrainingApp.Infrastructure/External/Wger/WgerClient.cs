using TrainingApp.Core.DTOs;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Infrastructure.External.Wger;

public class WgerClient : IWgerClient
{
    private readonly IWgerApi _api;

    public WgerClient(IWgerApi api)
    {
        _api = api;
    }

    public async Task<WgerExerciseResponse> GetExercisesAsync(int page = 1, string? language = "en", CancellationToken ct = default)
    {
        var offset = (page - 1) * 100;
        return await _api.GetExercisesAsync(limit: 100, offset: offset, language: language, ct);
    }

    public async Task<WgerExerciseDto?> GetExerciseAsync(int id, CancellationToken ct = default)
    {
        return await _api.GetExerciseAsync(id, ct);
    }

    public async Task<WgerCategoryResponse> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await _api.GetCategoriesAsync(ct);
    }

    public async Task<WgerMuscleResponse> GetMusclesAsync(CancellationToken ct = default)
    {
        return await _api.GetMusclesAsync(ct);
    }

    public async Task<WgerEquipmentResponse> GetEquipmentAsync(CancellationToken ct = default)
    {
        return await _api.GetEquipmentAsync(ct);
    }
}
