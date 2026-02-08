using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;

namespace TrainingApp.Api.Endpoints;

public static class CardioEndpoints
{
    public static void MapCardioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/cardio")
            .WithTags("Cardio");

        group.MapPost("/", LogCardio)
            .WithName("LogCardio")
            .WithSummary("Log a cardio session")
            .WithValidation<LogCardioRequest>();

        group.MapGet("/", GetCardioHistory)
            .WithName("GetCardioHistory")
            .WithSummary("Get cardio sessions for a date range");

        group.MapGet("/{id:guid}", GetCardioSession)
            .WithName("GetCardioSession")
            .WithSummary("Get a specific cardio session");

        group.MapDelete("/{id:guid}", DeleteCardioSession)
            .WithName("DeleteCardioSession")
            .WithSummary("Delete a cardio session");

        group.MapGet("/weekly", GetWeeklyCardioSummary)
            .WithName("GetWeeklyCardioSummary")
            .WithSummary("Get weekly cardio summary (computed)");
    }

    private static async Task<IResult> LogCardio(
        LogCardioRequest req,
        ICurrentUserService currentUser,
        ICardioTrackingService cardioService,
        CancellationToken ct)
    {
        var date = DateOnly.ParseExact(req.Date, "yyyy-MM-dd");
        var modality = Enum.Parse<CardioModality>(req.Modality, ignoreCase: true);
        var zone = Enum.Parse<CardioIntensityZone>(req.Zone, ignoreCase: true);
        DateTimeOffset? startedAt = req.StartedAt is not null ? DateTimeOffset.Parse(req.StartedAt) : null;

        var session = await cardioService.LogCardioAsync(
            currentUser.UserId, date, modality, zone, req.DurationMinutes,
            req.DistanceKm, req.AverageHeartRate, req.MaxHeartRate, startedAt, req.Notes, ct);

        return Results.Ok(ToResponse(session));
    }

    private static async Task<IResult> GetCardioHistory(
        string from, string to,
        ICurrentUserService currentUser,
        ICardioTrackingService cardioService,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var sessions = await cardioService.GetCardioHistoryAsync(currentUser.UserId, fromDate, toDate, ct);
        var items = sessions.Select(ToResponse).ToList();
        return Results.Ok(new CardioSessionListResponse(items, items.Count));
    }

    private static async Task<IResult> GetCardioSession(
        Guid id,
        ICurrentUserService currentUser,
        ICardioTrackingService cardioService,
        CancellationToken ct)
    {
        var session = await cardioService.GetCardioSessionAsync(currentUser.UserId, id, ct);
        return session is null ? Results.NotFound() : Results.Ok(ToResponse(session));
    }

    private static async Task<IResult> DeleteCardioSession(
        Guid id,
        ICurrentUserService currentUser,
        ICardioTrackingService cardioService,
        CancellationToken ct)
    {
        await cardioService.DeleteCardioAsync(currentUser.UserId, id, ct);
        return Results.Ok();
    }

    private static async Task<IResult> GetWeeklyCardioSummary(
        string weekStart,
        ICurrentUserService currentUser,
        ICardioTrackingService cardioService,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", out var startDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var endDate = startDate.AddDays(6);
        var sessions = await cardioService.GetCardioHistoryAsync(currentUser.UserId, startDate, endDate, ct);

        var sessionData = sessions.Select(s =>
            (s.Modality, s.Zone, s.DurationMinutes, s.DistanceKm, s.CardioTrimp ?? 0m)).ToList();

        var stats = InterferenceCalculatorService.CalculateWeeklySummary(sessionData);

        return Results.Ok(new WeeklyCardioSummaryResponse(
            startDate.ToString("yyyy-MM-dd"),
            stats.TotalSessions, stats.TotalMinutes, stats.TotalDistanceKm,
            stats.TotalTrimp, stats.MinutesByZone));
    }

    private static CardioSessionResponse ToResponse(CardioSession c) => new(
        c.Id, c.Date.ToString("yyyy-MM-dd"), c.Modality.ToString(), c.Zone.ToString(),
        c.DurationMinutes, c.DistanceKm, c.AverageHeartRate, c.MaxHeartRate,
        c.CardioTrimp, c.Notes,
        c.StartedAt?.ToString("o"), c.CompletedAt?.ToString("o"), c.CreatedAt);
}
