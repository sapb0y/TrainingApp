using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;

namespace TrainingApp.Api.Endpoints;

public static class FatigueEndpoints
{
    public static void MapFatigueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/fatigue")
            .WithTags("Fatigue");

        group.MapGet("/readiness", GetReadiness)
            .WithName("GetReadiness")
            .WithSummary("Get current readiness score and fatigue metrics");

        group.MapGet("/history", GetHistory)
            .WithName("GetFatigueHistory")
            .WithSummary("Get daily metrics history for a date range");

        group.MapPost("/recovery", LogRecovery)
            .WithName("LogRecovery")
            .WithSummary("Log recovery data for a date")
            .WithValidation<CreateRecoveryLogRequest>();

        group.MapGet("/recovery", GetRecoveryLogs)
            .WithName("GetRecoveryLogs")
            .WithSummary("Get recovery logs for a date range");

        group.MapPost("/recalculate", Recalculate)
            .WithName("RecalculateMetrics")
            .WithSummary("Recalculate daily metrics from workout history")
            .WithValidation<RecalculateRequest>();
    }

    private static async Task<IResult> GetReadiness(
        ICurrentUserService currentUser,
        IFatigueModelService fatigueService,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var metrics = await fatigueService.GetReadinessAsync(userId, ct);

        if (metrics is null)
        {
            // Return defaults when no data exists
            var defaultReadiness = TrainingStressService.CalculateReadiness(0m, Core.Entities.RecoveryCapacity.Normal);
            return Results.Ok(new ReadinessResponse(
                defaultReadiness.Score, defaultReadiness.Category, defaultReadiness.Recommendation,
                0m, 0m, 0m, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                null, []));
        }

        // Get today's recovery log
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recoveryLogs = await fatigueService.GetRecoveryLogsAsync(userId, today, today, ct);
        var todayRecovery = recoveryLogs.FirstOrDefault();

        // Get alerts
        var readiness = TrainingStressService.CalculateReadiness(
            metrics.Tsb, Core.Entities.RecoveryCapacity.Normal,
            todayRecovery?.SleepQuality, todayRecovery?.StressLevel,
            todayRecovery?.EnergyLevel, todayRecovery?.MuscleReadiness,
            todayRecovery?.Mood);

        var daysSinceDeload = 14; // simplified for endpoint response
        var alerts = FatigueRuleEngine.EvaluateRules(metrics.Tsb, metrics.ReadinessScore, daysSinceDeload, 0);

        return Results.Ok(new ReadinessResponse(
            metrics.ReadinessScore ?? readiness.Score,
            readiness.Category,
            readiness.Recommendation,
            metrics.Ctl,
            metrics.Atl,
            metrics.Tsb,
            metrics.Date.ToString("yyyy-MM-dd"),
            todayRecovery is not null ? ToRecoveryResponse(todayRecovery) : null,
            alerts.Select(a => new FatigueAlertResponse(a.RuleName, a.Severity.ToString(), a.Message)).ToList()));
    }

    private static async Task<IResult> GetHistory(
        string from,
        string to,
        ICurrentUserService currentUser,
        IFatigueModelService fatigueService,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var history = await fatigueService.GetHistoryAsync(userId, fromDate, toDate, ct);

        var items = history.Select(d => new DailyMetricsResponse(
            d.Id, d.Date.ToString("yyyy-MM-dd"), d.Trimp, d.Ctl, d.Atl, d.Tsb,
            d.TotalSets, d.TotalReps, d.TotalVolume, d.WorkoutCount,
            d.AverageSessionRpe, d.ReadinessScore)).ToList();

        return Results.Ok(new DailyMetricsListResponse(items, items.Count));
    }

    private static async Task<IResult> LogRecovery(
        CreateRecoveryLogRequest req,
        ICurrentUserService currentUser,
        IFatigueModelService fatigueService,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var date = DateOnly.ParseExact(req.Date, "yyyy-MM-dd");

        var log = await fatigueService.LogRecoveryAsync(
            userId, date, req.SleepQuality, req.SleepHours,
            req.StressLevel, req.EnergyLevel, req.MuscleReadiness,
            req.Mood, req.Notes, ct);

        return Results.Ok(ToRecoveryResponse(log));
    }

    private static async Task<IResult> GetRecoveryLogs(
        string from,
        string to,
        ICurrentUserService currentUser,
        IFatigueModelService fatigueService,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;

        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate) ||
            !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
        }

        var logs = await fatigueService.GetRecoveryLogsAsync(userId, fromDate, toDate, ct);

        var items = logs.Select(ToRecoveryResponse).ToList();
        return Results.Ok(new RecoveryLogListResponse(items, items.Count));
    }

    private static async Task<IResult> Recalculate(
        RecalculateRequest req,
        ICurrentUserService currentUser,
        IFatigueModelService fatigueService,
        CancellationToken ct)
    {
        var userId = currentUser.UserId;
        DateOnly? from = req.From is not null ? DateOnly.ParseExact(req.From, "yyyy-MM-dd") : null;

        await fatigueService.RecalculateAsync(userId, from, ct);

        return Results.Ok(new { message = "Recalculation complete" });
    }

    private static RecoveryLogResponse ToRecoveryResponse(Core.Entities.RecoveryLog r) => new(
        r.Id, r.Date.ToString("yyyy-MM-dd"), r.SleepQuality, r.SleepHours,
        r.StressLevel, r.EnergyLevel, r.MuscleReadiness, r.Mood, r.Notes, r.CreatedAt);
}
