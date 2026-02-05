using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Api.Endpoints;

public static class WorkoutEndpoints
{
    public static void MapWorkoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/workouts")
            .WithTags("Workouts");

        group.MapGet("/", GetAll)
            .WithName("GetWorkouts")
            .WithSummary("Get all workouts for current user");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetWorkoutById")
            .WithSummary("Get workout by ID");

        group.MapPost("/", Create)
            .WithName("CreateWorkout")
            .WithSummary("Create a new workout");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateWorkout")
            .WithSummary("Update an existing workout");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteWorkout")
            .WithSummary("Delete a workout");

        // Workout Sets
        group.MapGet("/{id:guid}/sets", GetSets)
            .WithName("GetWorkoutSets")
            .WithSummary("Get all sets for a workout");

        group.MapPost("/{id:guid}/sets", CreateSet)
            .WithName("CreateWorkoutSet")
            .WithSummary("Add a set to a workout");

        group.MapPut("/{id:guid}/sets/{setId:guid}", UpdateSet)
            .WithName("UpdateWorkoutSet")
            .WithSummary("Update a workout set");

        group.MapDelete("/{id:guid}/sets/{setId:guid}", DeleteSet)
            .WithName("DeleteWorkoutSet")
            .WithSummary("Delete a workout set");
    }

    // TODO: Replace with actual user ID from auth
    private static readonly Guid TempUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static async Task<IResult> GetAll(TrainingAppDbContext db, CancellationToken ct)
    {
        var workouts = await db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == TempUserId)
            .OrderByDescending(w => w.ScheduledAt)
            .Select(w => new WorkoutSummaryResponse(
                w.Id,
                w.Name,
                w.ScheduledAt,
                w.Status.ToString(),
                w.Sets.Count))
            .ToListAsync(ct);

        return Results.Ok(new WorkoutListResponse(workouts, workouts.Count));
    }

    private static async Task<IResult> GetById(Guid id, TrainingAppDbContext db, CancellationToken ct)
    {
        var workout = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == TempUserId, ct);

        return workout is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(workout));
    }

    private static async Task<IResult> Create(CreateWorkoutRequest req, TrainingAppDbContext db, CancellationToken ct)
    {
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            UserId = TempUserId,
            Name = req.Name,
            ScheduledAt = req.ScheduledAt,
            Notes = req.Notes,
            Status = WorkoutStatus.Planned
        };

        db.Workouts.Add(workout);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/workouts/{workout.Id}", ToResponse(workout));
    }

    private static async Task<IResult> Update(Guid id, UpdateWorkoutRequest req, TrainingAppDbContext db, CancellationToken ct)
    {
        var workout = await db.Workouts
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == TempUserId, ct);

        if (workout is null)
            return Results.NotFound();

        if (req.Name is not null) workout.Name = req.Name;
        if (req.ScheduledAt.HasValue) workout.ScheduledAt = req.ScheduledAt.Value;
        if (req.Notes is not null) workout.Notes = req.Notes;
        if (req.Status.HasValue) workout.Status = req.Status.Value;
        if (req.SessionRpe.HasValue) workout.SessionRpe = req.SessionRpe.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(workout));
    }

    private static async Task<IResult> Delete(Guid id, TrainingAppDbContext db, CancellationToken ct)
    {
        var workout = await db.Workouts
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == TempUserId, ct);

        if (workout is null)
            return Results.NotFound();

        db.Workouts.Remove(workout);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSets(Guid id, TrainingAppDbContext db, CancellationToken ct)
    {
        var workout = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == TempUserId, ct);

        if (workout is null)
            return Results.NotFound();

        return Results.Ok(workout.Sets.OrderBy(s => s.SetNumber).Select(ToSetResponse));
    }

    private static async Task<IResult> CreateSet(Guid id, CreateWorkoutSetRequest req, TrainingAppDbContext db, CancellationToken ct)
    {
        var workout = await db.Workouts
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == TempUserId, ct);

        if (workout is null)
            return Results.NotFound("Workout not found");

        var exercise = await db.Exercises.FindAsync([req.ExerciseId], ct);
        if (exercise is null)
            return Results.NotFound("Exercise not found");

        var set = new WorkoutSet
        {
            Id = Guid.NewGuid(),
            WorkoutId = id,
            ExerciseId = req.ExerciseId,
            SetNumber = req.SetNumber,
            TargetReps = req.TargetReps,
            TargetWeight = req.TargetWeight,
            IsWarmup = req.IsWarmup,
            Exercise = exercise
        };

        db.WorkoutSets.Add(set);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/workouts/{id}/sets/{set.Id}", ToSetResponse(set));
    }

    private static async Task<IResult> UpdateSet(Guid id, Guid setId, UpdateWorkoutSetRequest req, TrainingAppDbContext db, CancellationToken ct)
    {
        var set = await db.WorkoutSets
            .Include(s => s.Exercise)
            .Include(s => s.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId && s.WorkoutId == id && s.Workout!.UserId == TempUserId, ct);

        if (set is null)
            return Results.NotFound();

        if (req.TargetReps.HasValue) set.TargetReps = req.TargetReps.Value;
        if (req.TargetWeight.HasValue) set.TargetWeight = req.TargetWeight.Value;
        if (req.ActualReps.HasValue) set.ActualReps = req.ActualReps.Value;
        if (req.ActualWeight.HasValue) set.ActualWeight = req.ActualWeight.Value;
        if (req.Rpe.HasValue) set.Rpe = req.Rpe.Value;
        if (req.Rir.HasValue) set.Rir = req.Rir.Value;
        if (req.IsWarmup.HasValue) set.IsWarmup = req.IsWarmup.Value;
        if (req.CompletedAt.HasValue) set.CompletedAt = req.CompletedAt.Value;
        if (req.PerformedAt.HasValue) set.PerformedAt = req.PerformedAt.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSetResponse(set));
    }

    private static async Task<IResult> DeleteSet(Guid id, Guid setId, TrainingAppDbContext db, CancellationToken ct)
    {
        var set = await db.WorkoutSets
            .Include(s => s.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId && s.WorkoutId == id && s.Workout!.UserId == TempUserId, ct);

        if (set is null)
            return Results.NotFound();

        db.WorkoutSets.Remove(set);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static WorkoutResponse ToResponse(Workout w) => new(
        w.Id,
        w.UserId,
        w.Name,
        w.ScheduledAt,
        w.StartedAt,
        w.CompletedAt,
        w.Notes,
        w.Status.ToString(),
        w.SessionRpe,
        w.CreatedAt,
        w.Sets.OrderBy(s => s.SetNumber).Select(ToSetResponse).ToList());

    private static WorkoutSetResponse ToSetResponse(WorkoutSet s) => new(
        s.Id,
        s.ExerciseId,
        s.Exercise?.Name ?? "Unknown",
        s.SetNumber,
        s.TargetReps,
        s.TargetWeight,
        s.ActualReps,
        s.ActualWeight,
        s.Rpe,
        s.Rir,
        s.IsWarmup,
        s.CompletedAt,
        s.PerformedAt,
        s.EstimatedOneRepMax);
}
