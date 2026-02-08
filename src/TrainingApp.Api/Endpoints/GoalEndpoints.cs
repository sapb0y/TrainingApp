using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/goals")
            .WithTags("Goals");

        group.MapPost("/", CreateGoal)
            .WithName("CreateGoal")
            .WithSummary("Create a new goal")
            .WithValidation<CreateGoalRequest>();

        group.MapGet("/", ListGoals)
            .WithName("ListGoals")
            .WithSummary("List goals, optionally filtered by status");

        group.MapGet("/{id:guid}", GetGoalDetail)
            .WithName("GetGoalDetail")
            .WithSummary("Get goal detail with checkpoints");

        group.MapPut("/{id:guid}", UpdateGoal)
            .WithName("UpdateGoal")
            .WithSummary("Update a goal")
            .WithValidation<UpdateGoalRequest>();

        group.MapDelete("/{id:guid}", DeleteGoal)
            .WithName("DeleteGoal")
            .WithSummary("Delete a goal");

        group.MapPost("/{id:guid}/checkpoints", AddCheckpoint)
            .WithName("AddCheckpoint")
            .WithSummary("Add a checkpoint to a goal")
            .WithValidation<AddCheckpointRequest>();
    }

    private static async Task<IResult> CreateGoal(
        CreateGoalRequest req,
        ICurrentUserService currentUser,
        IGoalTrackingService goalService,
        CancellationToken ct)
    {
        var type = Enum.Parse<GoalType>(req.Type, ignoreCase: true);
        DateOnly? targetDate = req.TargetDate is not null
            ? DateOnly.ParseExact(req.TargetDate, "yyyy-MM-dd") : null;

        var goal = await goalService.CreateGoalAsync(
            currentUser.UserId, req.Title, type, req.TargetValue,
            req.TargetUnit, req.ExerciseId, req.StartValue, targetDate, req.Notes, ct);

        return Results.Ok(ToResponse(goal));
    }

    private static async Task<IResult> ListGoals(
        string? status,
        ICurrentUserService currentUser,
        IGoalTrackingService goalService,
        CancellationToken ct)
    {
        GoalStatus? statusFilter = null;
        if (status is not null && Enum.TryParse<GoalStatus>(status, ignoreCase: true, out var parsed))
            statusFilter = parsed;

        var goals = await goalService.GetGoalsAsync(currentUser.UserId, statusFilter, ct);
        var items = goals.Select(ToResponse).ToList();
        return Results.Ok(new GoalListResponse(items, items.Count));
    }

    private static async Task<IResult> GetGoalDetail(
        Guid id,
        ICurrentUserService currentUser,
        IGoalTrackingService goalService,
        CancellationToken ct)
    {
        var goal = await goalService.GetGoalAsync(currentUser.UserId, id, ct);
        if (goal is null)
            return Results.NotFound();

        var checkpoints = goal.Checkpoints
            .OrderBy(c => c.Date)
            .Select(c => new GoalCheckpointResponse(c.Id, c.Date.ToString("yyyy-MM-dd"), c.Value, c.PercentComplete, c.Notes))
            .ToList();

        return Results.Ok(new GoalDetailResponse(ToResponse(goal), checkpoints));
    }

    private static async Task<IResult> UpdateGoal(
        Guid id,
        UpdateGoalRequest req,
        ICurrentUserService currentUser,
        IGoalTrackingService goalService,
        CancellationToken ct)
    {
        GoalStatus? status = req.Status is not null
            ? Enum.Parse<GoalStatus>(req.Status, ignoreCase: true) : null;
        DateOnly? targetDate = req.TargetDate is not null
            ? DateOnly.ParseExact(req.TargetDate, "yyyy-MM-dd") : null;

        var goal = await goalService.UpdateGoalAsync(
            currentUser.UserId, id, status, req.TargetValue, targetDate, req.Notes, ct);

        return Results.Ok(ToResponse(goal));
    }

    private static async Task<IResult> DeleteGoal(
        Guid id,
        ICurrentUserService currentUser,
        IGoalTrackingService goalService,
        CancellationToken ct)
    {
        await goalService.DeleteGoalAsync(currentUser.UserId, id, ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddCheckpoint(
        Guid id,
        AddCheckpointRequest req,
        ICurrentUserService currentUser,
        IGoalTrackingService goalService,
        CancellationToken ct)
    {
        var date = DateOnly.ParseExact(req.Date, "yyyy-MM-dd");
        var checkpoint = await goalService.AddCheckpointAsync(
            currentUser.UserId, id, date, req.Value, req.Notes, ct);

        return Results.Ok(new GoalCheckpointResponse(
            checkpoint.Id, checkpoint.Date.ToString("yyyy-MM-dd"),
            checkpoint.Value, checkpoint.PercentComplete, checkpoint.Notes));
    }

    private static GoalResponse ToResponse(Goal g)
    {
        var latestCheckpoint = g.Checkpoints?.OrderByDescending(c => c.Date).FirstOrDefault();
        return new GoalResponse(
            g.Id, g.Title, g.Type.ToString(), g.Status.ToString(),
            g.TargetValue, g.TargetUnit, g.ExerciseId, g.StartValue,
            latestCheckpoint?.Value, latestCheckpoint?.PercentComplete,
            g.CreatedDate.ToString("yyyy-MM-dd"),
            g.TargetDate?.ToString("yyyy-MM-dd"),
            g.AchievedDate?.ToString("yyyy-MM-dd"),
            g.Notes, g.Checkpoints?.Count ?? 0, g.CreatedAt);
    }
}
