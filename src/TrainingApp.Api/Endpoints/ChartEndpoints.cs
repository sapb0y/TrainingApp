using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Api.Endpoints;

public static class ChartEndpoints
{
    public static void MapChartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/charts")
            .WithTags("Charts")
            .RequireAuthorization()
            .RequireTier(SubscriptionTier.Competitor);

        group.MapGet("/strength", GetStrengthChart)
            .WithName("GetStrengthChart")
            .WithSummary("Strength progression e1RM chart for an exercise");

        group.MapGet("/body-weight", GetBodyWeightChart)
            .WithName("GetBodyWeightChart")
            .WithSummary("Body weight with moving averages");

        group.MapGet("/volume", GetVolumeChart)
            .WithName("GetVolumeChart")
            .WithSummary("Volume and sets over time");

        group.MapGet("/cardio", GetCardioChart)
            .WithName("GetCardioChart")
            .WithSummary("Cardio duration, distance, and TRIMP");

        group.MapGet("/fatigue", GetFatigueChart)
            .WithName("GetFatigueChart")
            .WithSummary("CTL/ATL/TSB fatigue model curves");
    }

    private static async Task<IResult> GetStrengthChart(
        Guid exerciseId, string? from, string? to,
        ICurrentUserService currentUser, TrainingAppDbContext db, CancellationToken ct)
    {
        var (fromDate, toDate) = ParseDateRange(from, to);

        var points = await db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Workout)
            .Where(s => s.Workout!.UserId == currentUser.UserId
                && s.ExerciseId == exerciseId
                && !s.IsWarmup
                && s.ActualWeight != null && s.ActualReps != null
                && s.ActualReps >= 1 && s.ActualReps <= 30
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= fromDate
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) <= toDate)
            .Select(s => new
            {
                Date = DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime),
                E1rm = s.ActualWeight!.Value * (1 + s.ActualReps!.Value / 30m)
            })
            .ToListAsync(ct);

        // Group by date, take best e1RM per day
        var dailyBest = points
            .GroupBy(p => p.Date)
            .Select(g => (g.Key, g.Max(p => p.E1rm)))
            .OrderBy(p => p.Key)
            .ToList();

        var chart = ChartDataService.BuildStrengthProgressionChart(dailyBest);
        return Results.Ok(ToChartDataResponse(chart));
    }

    private static async Task<IResult> GetBodyWeightChart(
        string? from, string? to,
        ICurrentUserService currentUser, TrainingAppDbContext db, CancellationToken ct)
    {
        var (fromDate, toDate) = ParseDateRange(from, to);

        var logs = await db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == currentUser.UserId && w.Date >= fromDate && w.Date <= toDate)
            .OrderBy(w => w.Date)
            .Select(w => new { w.Date, w.WeightKg, w.MovingAverage7d, w.MovingAverage30d })
            .ToListAsync(ct);

        var points = logs.Select(w => (w.Date, w.WeightKg, w.MovingAverage7d, w.MovingAverage30d));
        var chart = ChartDataService.BuildBodyWeightChart(points);
        return Results.Ok(ToMultiSeriesResponse(chart));
    }

    private static async Task<IResult> GetVolumeChart(
        string? from, string? to, string? muscleGroup,
        ICurrentUserService currentUser, TrainingAppDbContext db, CancellationToken ct)
    {
        var (fromDate, toDate) = ParseDateRange(from, to);

        var query = db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Workout)
            .Include(s => s.Exercise)
            .Where(s => s.Workout!.UserId == currentUser.UserId
                && !s.IsWarmup
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= fromDate
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) <= toDate);

        if (!string.IsNullOrWhiteSpace(muscleGroup))
        {
            query = query.Where(s => s.Exercise!.PrimaryMuscles.Any(m =>
                m.ToLower() == muscleGroup.ToLower()));
        }

        var sets = await query
            .Select(s => new
            {
                Date = DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime),
                Volume = (s.ActualWeight ?? 0) * (s.ActualReps ?? 0)
            })
            .ToListAsync(ct);

        var dailyData = sets
            .GroupBy(s => s.Date)
            .Select(g => (g.Key, g.Sum(s => s.Volume), g.Count()))
            .OrderBy(p => p.Key)
            .ToList();

        var chart = ChartDataService.BuildVolumeChart(dailyData);
        return Results.Ok(ToMultiSeriesResponse(chart));
    }

    private static async Task<IResult> GetCardioChart(
        string? from, string? to,
        ICurrentUserService currentUser, TrainingAppDbContext db, CancellationToken ct)
    {
        var (fromDate, toDate) = ParseDateRange(from, to);

        var sessions = await db.CardioSessions
            .AsNoTracking()
            .Where(c => c.UserId == currentUser.UserId && c.Date >= fromDate && c.Date <= toDate)
            .OrderBy(c => c.Date)
            .Select(c => new { c.Date, c.DurationMinutes, c.DistanceKm, Trimp = c.CardioTrimp ?? 0m })
            .ToListAsync(ct);

        var points = sessions.Select(c => (c.Date, c.DurationMinutes, c.DistanceKm, c.Trimp));
        var chart = ChartDataService.BuildCardioChart(points);
        return Results.Ok(ToMultiSeriesResponse(chart));
    }

    private static async Task<IResult> GetFatigueChart(
        string? from, string? to,
        ICurrentUserService currentUser, TrainingAppDbContext db, CancellationToken ct)
    {
        var (fromDate, toDate) = ParseDateRange(from, to);

        var metrics = await db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == currentUser.UserId && m.Date >= fromDate && m.Date <= toDate)
            .OrderBy(m => m.Date)
            .Select(m => new { m.Date, m.Ctl, m.Atl, m.Tsb })
            .ToListAsync(ct);

        var points = metrics.Select(m => (m.Date, m.Ctl, m.Atl, m.Tsb));
        var chart = ChartDataService.BuildFatigueChart(points);
        return Results.Ok(ToMultiSeriesResponse(chart));
    }

    private static (DateOnly From, DateOnly To) ParseDateRange(string? from, string? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from is not null && DateOnly.TryParseExact(from, "yyyy-MM-dd", out var f)
            ? f : today.AddDays(-30);
        var toDate = to is not null && DateOnly.TryParseExact(to, "yyyy-MM-dd", out var t)
            ? t : today;
        return (fromDate, toDate);
    }

    private static ChartDataResponse ToChartDataResponse(ChartDataService.ChartData chart) =>
        new(ToSeriesResponse(chart.Series),
            new ChartStatsResponse(chart.Statistics.Min, chart.Statistics.Max, chart.Statistics.Average,
                chart.Statistics.Current, chart.Statistics.ChangePercent));

    private static MultiSeriesChartResponse ToMultiSeriesResponse(ChartDataService.MultiSeriesChartData chart) =>
        new(chart.Series.Select(ToSeriesResponse).ToList(), chart.Title);

    private static ChartSeriesResponse ToSeriesResponse(ChartDataService.ChartSeries series) =>
        new(series.Name, series.Unit,
            series.Points.Select(p => new ChartPointResponse(p.Date.ToString("yyyy-MM-dd"), p.Value, p.Label)).ToList());
}
