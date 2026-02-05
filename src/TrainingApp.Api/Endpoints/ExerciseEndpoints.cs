using TrainingApp.Api.Contracts;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class ExerciseEndpoints
{
    public static void MapExerciseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/exercises")
            .WithTags("Exercises");

        group.MapGet("/", GetAll)
            .WithName("GetExercises")
            .WithSummary("Get all exercises");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetExerciseById")
            .WithSummary("Get exercise by ID");

        group.MapGet("/search", Search)
            .WithName("SearchExercises")
            .WithSummary("Search exercises by name or category");

        group.MapPost("/sync", Sync)
            .WithName("SyncExercises")
            .WithSummary("Sync exercises from wger API");
    }

    private static async Task<IResult> GetAll(IExerciseCacheService cache, CancellationToken ct)
    {
        var exercises = await cache.GetAllExercisesAsync(ct);
        var response = new ExerciseListResponse(
            exercises.Select(ToResponse).ToList(),
            exercises.Count);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetById(Guid id, IExerciseCacheService cache, CancellationToken ct)
    {
        var exercise = await cache.GetByIdAsync(id, ct);
        return exercise is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(exercise));
    }

    private static async Task<IResult> Search(string q, IExerciseCacheService cache, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Results.BadRequest("Query must be at least 2 characters");

        var exercises = await cache.SearchAsync(q, ct);
        var response = new ExerciseListResponse(
            exercises.Select(ToResponse).ToList(),
            exercises.Count);
        return Results.Ok(response);
    }

    private static async Task<IResult> Sync(IExerciseCacheService cache, CancellationToken ct)
    {
        var count = await cache.SyncFromWgerAsync(ct);
        return Results.Ok(new ExerciseSyncResponse(count, DateTimeOffset.UtcNow));
    }

    private static ExerciseResponse ToResponse(Core.Entities.Exercise e) => new(
        e.Id,
        e.WgerId,
        e.Name,
        e.Description,
        e.Category,
        e.PrimaryMuscles,
        e.SecondaryMuscles,
        e.Equipment,
        e.IsCustom,
        e.CachedAt);
}
