using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Api.Endpoints;

public static class WorkoutEndpoints
{
    public static void MapWorkoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/workouts")
            .WithTags("Workouts")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetWorkouts")
            .WithSummary("Get all workouts for current user");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetWorkoutById")
            .WithSummary("Get workout by ID");

        group.MapPost("/", Create)
            .WithName("CreateWorkout")
            .WithSummary("Create a new workout")
            .WithValidation<CreateWorkoutRequest>();

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateWorkout")
            .WithSummary("Update an existing workout")
            .WithValidation<UpdateWorkoutRequest>();

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteWorkout")
            .WithSummary("Delete a workout");

        // Workout lifecycle
        group.MapPost("/{id:guid}/start", StartWorkout)
            .WithName("StartWorkout")
            .WithSummary("Start a workout")
            .WithValidation<StartWorkoutRequest>();

        group.MapPost("/{id:guid}/complete", CompleteWorkout)
            .WithName("CompleteWorkout")
            .WithSummary("Complete a workout")
            .WithValidation<CompleteWorkoutRequest>();

        // Autoregulation
        group.MapGet("/{id:guid}/recommendations", GetRecommendations)
            .WithName("GetWorkoutRecommendations")
            .WithSummary("Get autoregulation recommendations for a workout");

        group.MapGet("/{id:guid}/adaptation-log", GetAdaptationLog)
            .WithName("GetAdaptationLog")
            .WithSummary("Get adaptation log entries for a workout");

        // Workout Sets
        group.MapGet("/{id:guid}/sets", GetSets)
            .WithName("GetWorkoutSets")
            .WithSummary("Get all sets for a workout");

        group.MapPost("/{id:guid}/sets", CreateSet)
            .WithName("CreateWorkoutSet")
            .WithSummary("Add a set to a workout")
            .WithValidation<CreateWorkoutSetRequest>();

        group.MapPut("/{id:guid}/sets/{setId:guid}", UpdateSet)
            .WithName("UpdateWorkoutSet")
            .WithSummary("Update a workout set")
            .WithValidation<UpdateWorkoutSetRequest>();

        group.MapDelete("/{id:guid}/sets/{setId:guid}", DeleteSet)
            .WithName("DeleteWorkoutSet")
            .WithSummary("Delete a workout set");
    }

    private static async Task<IResult> GetAll(
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workouts = await db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == userId)
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

    private static async Task<IResult> GetById(
        Guid id,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (workout is null)
            throw new NotFoundException("Workout", id.ToString());

        return Results.Ok(ToResponse(workout));
    }

    private static async Task<IResult> Create(
        CreateWorkoutRequest req,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = req.Name,
            ScheduledAt = req.ScheduledAt,
            Notes = req.Notes,
            Status = WorkoutStatus.Planned
        };

        db.Workouts.Add(workout);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/workouts/{workout.Id}", ToResponse(workout));
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateWorkoutRequest req,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = await db.Workouts
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (workout is null)
            throw new NotFoundException("Workout", id.ToString());

        if (req.Name is not null) workout.Name = req.Name;
        if (req.ScheduledAt.HasValue) workout.ScheduledAt = req.ScheduledAt.Value;
        if (req.Notes is not null) workout.Notes = req.Notes;
        if (req.Status.HasValue) workout.Status = req.Status.Value;
        if (req.SessionRpe.HasValue) workout.SessionRpe = req.SessionRpe.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(workout));
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = await db.Workouts
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (workout is null)
            throw new NotFoundException("Workout", id.ToString());

        db.Workouts.Remove(workout);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> StartWorkout(
        Guid id,
        StartWorkoutRequest req,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = await db.Workouts
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (workout is null)
            throw new NotFoundException("Workout", id.ToString());

        workout.Status = WorkoutStatus.InProgress;
        workout.StartedAt = DateTimeOffset.UtcNow;
        workout.PreWorkoutReadiness = req.PreWorkoutReadiness;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(workout));
    }

    private static async Task<IResult> CompleteWorkout(
        Guid id,
        CompleteWorkoutRequest req,
        ICurrentUserService currentUser,
        IFatigueModelService fatigueService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = await db.Workouts
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (workout is null)
            throw new NotFoundException("Workout", id.ToString());

        workout.Status = WorkoutStatus.Completed;
        workout.CompletedAt = DateTimeOffset.UtcNow;
        workout.SessionRpe = req.SessionRpe;
        workout.PostWorkoutFatigue = req.PostWorkoutFatigue;
        if (req.Notes is not null) workout.Notes = req.Notes;

        await db.SaveChangesAsync(ct);

        // Auto-trigger daily metrics update
        try
        {
            var completedDate = DateOnly.FromDateTime(workout.CompletedAt!.Value.UtcDateTime);
            await fatigueService.UpdateDailyMetricsAsync(userId, completedDate, ct);
        }
        catch (Exception)
        {
            // Don't fail the workout completion if metrics update fails
            // DailyMetricsJob will pick it up as backup
        }

        return Results.Ok(ToResponse(workout));
    }

    private static async Task<IResult> GetRecommendations(
        Guid id,
        ICurrentUserService currentUser,
        IAutoregulationExecutionService autoregService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        // Verify ownership
        var exists = await db.Workouts.AnyAsync(w => w.Id == id && w.UserId == userId, ct);
        if (!exists)
            throw new NotFoundException("Workout", id.ToString());

        var evaluation = await autoregService.GetRecommendationsAsync(id, ct);

        var exerciseIds = evaluation.Exercises.Select(e => e.ExerciseId).Distinct().ToList();
        var exerciseNames = await db.Exercises.AsNoTracking()
            .Where(ex => exerciseIds.Contains(ex.Id))
            .ToDictionaryAsync(ex => ex.Id, ex => ex.Name, ct);

        var response = new WorkoutRecommendationsResponse(
            evaluation.Exercises.Select(e => new ExerciseRecommendationResponse(
                e.ExerciseId,
                exerciseNames.GetValueOrDefault(e.ExerciseId, "Unknown"),
                e.Volume.ToString(),
                e.NextSet is not null ? new NextSetResponse(e.NextSet.Weight, e.NextSet.Reps, e.NextSet.TargetRir) : null,
                e.Reason)).ToList(),
            evaluation.SessionNote);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAdaptationLog(
        Guid id,
        ICurrentUserService currentUser,
        IAutoregulationExecutionService autoregService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var exists = await db.Workouts.AnyAsync(w => w.Id == id && w.UserId == userId, ct);
        if (!exists)
            throw new NotFoundException("Workout", id.ToString());

        var logs = await autoregService.GetAdaptationLogAsync(id, ct);

        return Results.Ok(logs.Select(l => new AdaptationLogResponse(
            l.Id, l.RuleName, l.Scope, l.InputSummary, l.OutputSummary, l.WasApplied, l.CreatedAt)));
    }

    private static async Task<IResult> GetSets(
        Guid id,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (workout is null)
            throw new NotFoundException("Workout", id.ToString());

        return Results.Ok(workout.Sets.OrderBy(s => s.SetNumber).Select(ToSetResponse));
    }

    private static async Task<IResult> CreateSet(
        Guid id,
        CreateWorkoutSetRequest req,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var workout = await db.Workouts
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId, ct);

        if (workout is null)
            throw new NotFoundException("Workout", id.ToString());

        var exercise = await db.Exercises.FindAsync([req.ExerciseId], ct);
        if (exercise is null)
            throw new NotFoundException("Exercise", req.ExerciseId.ToString());

        var set = new WorkoutSet
        {
            Id = Guid.NewGuid(),
            WorkoutId = id,
            ExerciseId = req.ExerciseId,
            SetNumber = req.SetNumber,
            TargetReps = req.TargetReps,
            TargetWeight = req.TargetWeight,
            TargetRir = req.TargetRir,
            IsWarmup = req.IsWarmup,
            Exercise = exercise
        };

        db.WorkoutSets.Add(set);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/workouts/{id}/sets/{set.Id}", ToSetResponse(set));
    }

    private static async Task<IResult> UpdateSet(
        Guid id,
        Guid setId,
        UpdateWorkoutSetRequest req,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var set = await db.WorkoutSets
            .Include(s => s.Exercise)
            .Include(s => s.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId && s.WorkoutId == id && s.Workout!.UserId == userId, ct);

        if (set is null)
            throw new NotFoundException("WorkoutSet", setId.ToString());

        if (req.TargetReps.HasValue) set.TargetReps = req.TargetReps.Value;
        if (req.TargetWeight.HasValue) set.TargetWeight = req.TargetWeight.Value;
        if (req.ActualReps.HasValue) set.ActualReps = req.ActualReps.Value;
        if (req.ActualWeight.HasValue) set.ActualWeight = req.ActualWeight.Value;
        if (req.Rpe.HasValue) set.Rpe = req.Rpe.Value;
        if (req.Rir.HasValue) set.Rir = req.Rir.Value;
        if (req.TargetRir.HasValue) set.TargetRir = req.TargetRir.Value;
        if (req.IsWarmup.HasValue) set.IsWarmup = req.IsWarmup.Value;
        if (req.CompletedAt.HasValue) set.CompletedAt = req.CompletedAt.Value;
        if (req.PerformedAt.HasValue) set.PerformedAt = req.PerformedAt.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok(ToSetResponse(set));
    }

    private static async Task<IResult> DeleteSet(
        Guid id,
        Guid setId,
        ICurrentUserService currentUser,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var set = await db.WorkoutSets
            .Include(s => s.Workout)
            .FirstOrDefaultAsync(s => s.Id == setId && s.WorkoutId == id && s.Workout!.UserId == userId, ct);

        if (set is null)
            throw new NotFoundException("WorkoutSet", setId.ToString());

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
        w.PreWorkoutReadiness,
        w.PostWorkoutFatigue,
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
        s.TargetRir,
        s.RirDrift,
        s.IsWarmup,
        s.WasAutoAdjusted,
        s.AdjustmentReason,
        s.CompletedAt,
        s.PerformedAt,
        s.EstimatedOneRepMax);
}
