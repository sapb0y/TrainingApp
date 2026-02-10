using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class SharedSessionEndpoints
{
    public static void MapSharedSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/shared-sessions")
            .WithTags("Shared Sessions")
            .RequireAuthorization()
            .RequireTier(SubscriptionTier.Competitor);

        group.MapPost("/", CreateSession)
            .WithName("CreateSharedSession")
            .WithSummary("Create a shared session")
            .WithValidation<CreateSharedSessionRequest>();

        group.MapGet("/{id:guid}", GetSession)
            .WithName("GetSharedSession")
            .WithSummary("Get shared session details");

        group.MapGet("/", ListSessions)
            .WithName("ListSharedSessions")
            .WithSummary("List shared sessions for a partnership");

        group.MapPost("/{id:guid}/generate", GenerateSchedule)
            .WithName("GenerateSchedule")
            .WithSummary("Generate interleaved schedule for a shared session");

        group.MapPost("/{id:guid}/cancel", CancelSession)
            .WithName("CancelSharedSession")
            .WithSummary("Cancel a shared session");
    }

    private static async Task<IResult> CreateSession(
        CreateSharedSessionRequest req,
        ICurrentUserService currentUser,
        ISharedSessionService sessionService,
        CancellationToken ct)
    {
        var date = DateOnly.ParseExact(req.Date, "yyyy-MM-dd");
        var session = await sessionService.CreateSessionAsync(
            currentUser.UserId, req.PartnershipId, date, req.WorkoutAId, req.WorkoutBId, req.Notes, ct);
        return Results.Ok(ToResponse(session));
    }

    private static async Task<IResult> GetSession(
        Guid id,
        ICurrentUserService currentUser,
        ISharedSessionService sessionService,
        CancellationToken ct)
    {
        var session = await sessionService.GetSessionAsync(currentUser.UserId, id, ct);
        if (session is null)
            return Results.NotFound();
        return Results.Ok(ToResponse(session));
    }

    private static async Task<IResult> ListSessions(
        Guid partnershipId,
        ICurrentUserService currentUser,
        ISharedSessionService sessionService,
        CancellationToken ct)
    {
        var sessions = await sessionService.GetSessionsAsync(currentUser.UserId, partnershipId, ct);
        var items = sessions.Select(ToResponse).ToList();
        return Results.Ok(new SharedSessionListResponse(items, items.Count));
    }

    private static async Task<IResult> GenerateSchedule(
        Guid id,
        ICurrentUserService currentUser,
        ISharedSessionService sessionService,
        CancellationToken ct)
    {
        var session = await sessionService.GenerateScheduleAsync(currentUser.UserId, id, ct);
        return Results.Ok(ToResponse(session));
    }

    private static async Task<IResult> CancelSession(
        Guid id,
        ICurrentUserService currentUser,
        ISharedSessionService sessionService,
        CancellationToken ct)
    {
        var session = await sessionService.CancelSessionAsync(currentUser.UserId, id, ct);
        return Results.Ok(ToResponse(session));
    }

    private static SharedSessionResponse ToResponse(SharedSession s)
    {
        var schedule = s.Slots?.OrderBy(sl => sl.SlotOrder).Select(sl =>
            new ScheduleBlockResponse(
                sl.SlotOrder,
                new SlotActionResponse(sl.UserAExerciseId, null, sl.UserASetNumber, sl.UserAAction ?? "Idle", null),
                new SlotActionResponse(sl.UserBExerciseId, null, sl.UserBSetNumber, sl.UserBAction ?? "Idle", null),
                sl.DurationSeconds)).ToList();

        return new SharedSessionResponse(
            s.Id,
            s.PartnershipId,
            s.ScheduledDate.ToString("yyyy-MM-dd"),
            s.Status.ToString(),
            s.WorkoutAId,
            s.WorkoutBId,
            s.EstimatedDurationMinutes,
            s.SoloEstimateMinutesA,
            s.SoloEstimateMinutesB,
            s.Notes,
            s.CreatedAt,
            schedule?.Count > 0 ? schedule : null,
            null,
            null);
    }
}
