using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;

namespace TrainingApp.Api.Endpoints;

public static class WeightEndpoints
{
    public static void MapWeightEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/weight")
            .WithTags("Weight")
            .RequireAuthorization();

        group.MapPost("/", LogWeight)
            .WithName("LogWeight")
            .WithSummary("Log a weight measurement")
            .WithValidation<LogWeightRequest>();

        group.MapGet("/", GetWeightHistory)
            .WithName("GetWeightHistory")
            .WithSummary("Get weight history for a date range");

        group.MapGet("/latest", GetLatestWeight)
            .WithName("GetLatestWeight")
            .WithSummary("Get most recent weight log");

        group.MapGet("/projection", GetProjection)
            .WithName("GetWeightProjection")
            .WithSummary("Get weight projection based on current rate");
    }

    private static async Task<IResult> LogWeight(
        LogWeightRequest req,
        ICurrentUserService currentUser,
        IWeightTrackingService weightService,
        CancellationToken ct)
    {
        var date = DateOnly.ParseExact(req.Date, "yyyy-MM-dd");
        var log = await weightService.LogWeightAsync(currentUser.UserId, date, req.WeightKg, req.Notes, ct);
        return Results.Ok(ToResponse(log));
    }

    private static async Task<IResult> GetWeightHistory(
        string from,
        string to,
        ICurrentUserService currentUser,
        IWeightTrackingService weightService,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var logs = await weightService.GetWeightHistoryAsync(currentUser.UserId, fromDate, toDate, ct);
        var items = logs.Select(ToResponse).ToList();
        return Results.Ok(new WeightLogListResponse(items, items.Count));
    }

    private static async Task<IResult> GetLatestWeight(
        ICurrentUserService currentUser,
        IWeightTrackingService weightService,
        CancellationToken ct)
    {
        var log = await weightService.GetLatestWeightAsync(currentUser.UserId, ct);
        return log is null ? Results.NotFound() : Results.Ok(ToResponse(log));
    }

    private static async Task<IResult> GetProjection(
        int? weeks,
        ICurrentUserService currentUser,
        IWeightTrackingService weightService,
        CancellationToken ct)
    {
        var latest = await weightService.GetLatestWeightAsync(currentUser.UserId, ct);
        if (latest is null)
            return Results.NotFound(new { error = "No weight data available for projection." });

        var projectionWeeks = weeks ?? 12;
        var rate = latest.WeeklyRateKg ?? 0m;
        var projections = MetabolicCalculatorService.ProjectWeight(latest.WeightKg, rate, projectionWeeks);

        var items = projections.Select(p => new WeightProjectionResponse(p.Date.ToString("yyyy-MM-dd"), p.ProjectedWeightKg)).ToList();
        return Results.Ok(items);
    }

    private static WeightLogResponse ToResponse(Core.Entities.WeightLog w) => new(
        w.Id, w.Date.ToString("yyyy-MM-dd"), w.WeightKg,
        w.MovingAverage7d, w.MovingAverage30d, w.WeeklyRateKg,
        w.Notes, w.CreatedAt);
}
