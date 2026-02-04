using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.DTOs;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class ExerciseCacheService : IExerciseCacheService
{
    private readonly TrainingAppDbContext _db;
    private readonly IWgerClient _wgerClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExerciseCacheService> _logger;
    private const string AllExercisesCacheKey = "exercises_all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public ExerciseCacheService(
        TrainingAppDbContext db,
        IWgerClient wgerClient,
        IMemoryCache cache,
        ILogger<ExerciseCacheService> logger)
    {
        _db = db;
        _wgerClient = wgerClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Exercise>> GetAllExercisesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(AllExercisesCacheKey, out IReadOnlyList<Exercise>? cached) && cached is not null)
            return cached;

        var exercises = await _db.Exercises
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(ct);

        _cache.Set(AllExercisesCacheKey, (IReadOnlyList<Exercise>)exercises, CacheDuration);
        return exercises;
    }

    public async Task<Exercise?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Exercise?> GetByWgerIdAsync(int wgerId, CancellationToken ct = default)
    {
        return await _db.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.WgerId == wgerId, ct);
    }

    public async Task<IReadOnlyList<Exercise>> SearchAsync(string query, CancellationToken ct = default)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _db.Exercises
            .AsNoTracking()
            .Where(e => EF.Functions.ILike(e.Name, $"%{query}%") ||
                       EF.Functions.ILike(e.Category, $"%{query}%"))
            .OrderBy(e => e.Name)
            .Take(50)
            .ToListAsync(ct);
    }

    public async Task<int> SyncFromWgerAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting wger exercise sync");

        var categories = await _wgerClient.GetCategoriesAsync(ct);
        var muscles = await _wgerClient.GetMusclesAsync(ct);
        var equipment = await _wgerClient.GetEquipmentAsync(ct);

        var categoryMap = categories.Results.ToDictionary(c => c.Id, c => c.Name);
        var muscleMap = muscles.Results.ToDictionary(m => m.Id, m => m.NameEn ?? m.Name);
        var equipmentMap = equipment.Results.ToDictionary(e => e.Id, e => e.Name);

        var syncCount = 0;
        var page = 1;
        var hasMore = true;

        while (hasMore)
        {
            var response = await _wgerClient.GetExercisesAsync(page, "en", ct);

            foreach (var dto in response.Results)
            {
                var existing = await _db.Exercises
                    .FirstOrDefaultAsync(e => e.WgerId == dto.Id, ct);

                var exercise = existing ?? new Exercise
                {
                    Id = Guid.NewGuid(),
                    WgerId = dto.Id,
                    Name = dto.Name,
                    Category = categoryMap.GetValueOrDefault(dto.Category, "Unknown"),
                    IsCustom = false
                };

                exercise.Name = dto.Name;
                exercise.Description = dto.Description;
                exercise.Category = categoryMap.GetValueOrDefault(dto.Category, "Unknown");
                exercise.PrimaryMuscles = dto.Muscles.Select(m => muscleMap.GetValueOrDefault(m, "Unknown")).ToList();
                exercise.SecondaryMuscles = dto.MusclesSecondary.Select(m => muscleMap.GetValueOrDefault(m, "Unknown")).ToList();
                exercise.Equipment = dto.Equipment.Select(e => equipmentMap.GetValueOrDefault(e, "Unknown")).ToList();
                exercise.CachedAt = DateTimeOffset.UtcNow;

                if (existing is null)
                    _db.Exercises.Add(exercise);

                syncCount++;
            }

            await _db.SaveChangesAsync(ct);

            hasMore = response.Next is not null;
            page++;

            if (page > 50) break; // Safety limit
        }

        _cache.Remove(AllExercisesCacheKey);
        _logger.LogInformation("Synced {Count} exercises from wger", syncCount);

        return syncCount;
    }
}
