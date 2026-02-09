using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class DeficitEndpoints
{
    public static void MapDeficitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/deficit")
            .WithTags("Deficit")
            .RequireAuthorization();

        group.MapPost("/start", StartDeficit)
            .WithName("StartDeficit")
            .WithSummary("Start a new deficit phase")
            .WithValidation<StartDeficitRequest>();

        group.MapGet("/active", GetActiveDeficit)
            .WithName("GetActiveDeficit")
            .WithSummary("Get the active deficit phase");

        group.MapPost("/end", EndDeficit)
            .WithName("EndDeficit")
            .WithSummary("End the active deficit phase");

        group.MapPost("/pause", PauseDeficit)
            .WithName("PauseDeficit")
            .WithSummary("Pause the active deficit phase");

        group.MapPost("/resume", ResumeDeficit)
            .WithName("ResumeDeficit")
            .WithSummary("Resume a paused deficit phase");

        group.MapGet("/history", GetDeficitHistory)
            .WithName("GetDeficitHistory")
            .WithSummary("Get all deficit phases");
    }

    private static async Task<IResult> StartDeficit(
        StartDeficitRequest req,
        ICurrentUserService currentUser,
        IDeficitPhaseService deficitService,
        CancellationToken ct)
    {
        var strategy = Enum.Parse<DeficitStrategy>(req.Strategy, ignoreCase: true);

        var phase = await deficitService.StartDeficitAsync(
            currentUser.UserId, req.StartWeightKg, req.TargetWeightKg,
            req.WeeklyRateKg, strategy, req.DietBreakIntervalWeeks, req.Notes, ct);
        return Results.Ok(ToResponse(phase));
    }

    private static async Task<IResult> GetActiveDeficit(
        ICurrentUserService currentUser,
        IDeficitPhaseService deficitService,
        CancellationToken ct)
    {
        var phase = await deficitService.GetActiveDeficitAsync(currentUser.UserId, ct);
        return phase is null ? Results.NotFound() : Results.Ok(ToResponse(phase));
    }

    private static async Task<IResult> EndDeficit(
        ICurrentUserService currentUser,
        IDeficitPhaseService deficitService,
        CancellationToken ct)
    {
        var phase = await deficitService.EndDeficitAsync(currentUser.UserId, ct);
        return Results.Ok(ToResponse(phase));
    }

    private static async Task<IResult> PauseDeficit(
        ICurrentUserService currentUser,
        IDeficitPhaseService deficitService,
        CancellationToken ct)
    {
        var phase = await deficitService.PauseDeficitAsync(currentUser.UserId, ct);
        return Results.Ok(ToResponse(phase));
    }

    private static async Task<IResult> ResumeDeficit(
        ICurrentUserService currentUser,
        IDeficitPhaseService deficitService,
        CancellationToken ct)
    {
        var phase = await deficitService.ResumeDeficitAsync(currentUser.UserId, ct);
        return Results.Ok(ToResponse(phase));
    }

    private static async Task<IResult> GetDeficitHistory(
        ICurrentUserService currentUser,
        IDeficitPhaseService deficitService,
        CancellationToken ct)
    {
        var phases = await deficitService.GetDeficitHistoryAsync(currentUser.UserId, ct);
        var items = phases.Select(ToResponse).ToList();
        return Results.Ok(new DeficitPhaseListResponse(items, items.Count));
    }

    private static DeficitPhaseResponse ToResponse(DeficitPhase d) => new(
        d.Id, d.StartDate.ToString("yyyy-MM-dd"), d.EndDate?.ToString("yyyy-MM-dd"),
        d.StartWeightKg, d.TargetWeightKg, d.WeeklyRateKg,
        d.Strategy.ToString(), d.Status.ToString(),
        d.DietBreakIntervalWeeks, d.LastDietBreakDate?.ToString("yyyy-MM-dd"),
        d.CurrentAdaptationPercent, d.Notes, d.CreatedAt);
}
