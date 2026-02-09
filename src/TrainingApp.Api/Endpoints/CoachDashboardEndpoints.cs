using Microsoft.EntityFrameworkCore;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Api.Endpoints;

public static class CoachDashboardEndpoints
{
    public static void MapCoachDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/coach")
            .WithTags("Coach Dashboard")
            .RequireAuthorization();

        group.MapGet("/dashboard", GetDashboard)
            .WithName("GetCoachDashboard")
            .WithSummary("Coach dashboard with roster and alerts");

        group.MapGet("/athletes/{id:guid}/dashboard", GetAthleteDashboard)
            .WithName("GetCoachAthleteDashboard")
            .WithSummary("Full single-athlete dashboard");

        group.MapGet("/athletes/{id:guid}/workouts", GetAthleteWorkouts)
            .WithName("GetCoachAthleteWorkouts")
            .WithSummary("Athlete workout history");

        group.MapGet("/athletes/{id:guid}/charts/{type}", GetAthleteChart)
            .WithName("GetCoachAthleteChart")
            .WithSummary("Athlete chart data");
    }

    private static async Task<IResult> GetDashboard(
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        ICoachAlertAggregatorService alertService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view dashboard.");

        var coachId = currentUser.UserId;
        var athletes = await coachService.GetAthletesAsync(coachId, ct);
        var alerts = await alertService.GetAlertsForAllAthletesAsync(coachId, ct);

        var alertsByAthlete = alerts.GroupBy(a => a.AthleteId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var roster = new List<CoachAthleteRosterItem>();
        foreach (var rel in athletes)
        {
            if (rel.AthleteId is null) continue;
            var athleteId = rel.AthleteId.Value;
            var athleteName = rel.Athlete?.DisplayName ?? "Unknown";

            var latestMetrics = await db.DailyMetrics
                .AsNoTracking()
                .Where(m => m.UserId == athleteId)
                .OrderByDescending(m => m.Date)
                .FirstOrDefaultAsync(ct);

            string? readinessCategory = null;
            if (latestMetrics?.ReadinessScore is not null)
            {
                readinessCategory = latestMetrics.ReadinessScore.Value switch
                {
                    >= 8m => "Excellent",
                    >= 6m => "Good",
                    >= 4m => "Moderate",
                    >= 3m => "Poor",
                    _ => "Very Poor"
                };
            }

            var lastWorkout = await db.Workouts
                .AsNoTracking()
                .Where(w => w.UserId == athleteId && w.Status == WorkoutStatus.Completed)
                .OrderByDescending(w => w.CompletedAt)
                .Select(w => w.CompletedAt)
                .FirstOrDefaultAsync(ct);

            var athleteAlertCount = alertsByAthlete.TryGetValue(athleteId, out var aa) ? aa.Count : 0;

            roster.Add(new CoachAthleteRosterItem(
                athleteId, athleteName, latestMetrics?.Tsb, readinessCategory,
                athleteAlertCount, lastWorkout?.ToString("o")));
        }

        var alertResponses = alerts.Select(a => new CoachAlertResponse(
            a.AthleteId, a.AthleteName, a.Category.ToString(), a.Severity, a.Message, a.Data)).ToList();

        var needingAttention = alertsByAthlete.Count(kv => kv.Value.Any(a => a.Severity is "Alert" or "Warning"));

        return Results.Ok(new CoachDashboardResponse(roster, alertResponses, athletes.Count, needingAttention));
    }

    private static async Task<IResult> GetAthleteDashboard(
        Guid id,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        ICoachAlertAggregatorService alertService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view athlete dashboards.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var athlete = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id.ToString());

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = today.AddDays(-7);
        var thirtyDaysAgo = today.AddDays(-30);
        var sixtyDaysAgo = today.AddDays(-60);

        // Fatigue
        var latestMetrics = await db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == id)
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync(ct);

        string? readinessCategory = null;
        if (latestMetrics?.ReadinessScore is not null)
        {
            readinessCategory = latestMetrics.ReadinessScore.Value switch
            {
                >= 8m => "Excellent",
                >= 6m => "Good",
                >= 4m => "Moderate",
                >= 3m => "Poor",
                _ => "Very Poor"
            };
        }

        var fatigue = new FatigueSnapshotResponse(
            latestMetrics?.Ctl, latestMetrics?.Atl, latestMetrics?.Tsb, readinessCategory);

        // Volume
        var last7dMetrics = await db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == id && m.Date >= sevenDaysAgo)
            .ToListAsync(ct);

        var prev7dMetrics = await db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == id && m.Date >= sevenDaysAgo.AddDays(-7) && m.Date < sevenDaysAgo)
            .ToListAsync(ct);

        var totalSets7d = last7dMetrics.Sum(m => m.TotalSets);
        var totalVolume7d = last7dMetrics.Sum(m => m.TotalVolume);
        var prevVolume7d = prev7dMetrics.Sum(m => m.TotalVolume);
        decimal? volumeChangePct = prevVolume7d > 0
            ? Math.Round((totalVolume7d - prevVolume7d) / prevVolume7d * 100m, 1)
            : null;

        var volume = new VolumeSnapshotResponse(totalSets7d, totalVolume7d > 0 ? totalVolume7d : null, volumeChangePct);

        // Strength
        var recentSets = await db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Workout)
            .Include(s => s.Exercise)
            .Where(s => s.Workout!.UserId == id
                && !s.IsWarmup
                && s.ActualWeight != null && s.ActualReps != null
                && s.ActualReps >= 1 && s.ActualReps <= 30
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= sixtyDaysAgo)
            .Select(s => new
            {
                Date = DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime),
                ExerciseName = s.Exercise!.Name,
                E1rm = s.ActualWeight!.Value * (1 + s.ActualReps!.Value / 30m)
            })
            .ToListAsync(ct);

        var exerciseBests = recentSets
            .Where(s => s.Date >= thirtyDaysAgo)
            .GroupBy(s => s.ExerciseName)
            .Select(g => new { Name = g.Key, BestE1rm = g.Max(s => s.E1rm) })
            .ToList();

        var topExercise = exerciseBests.OrderByDescending(e => e.BestE1rm).FirstOrDefault();
        decimal? e1rmChangePct = null;
        if (topExercise is not null)
        {
            var prevBest = recentSets
                .Where(s => s.ExerciseName == topExercise.Name && s.Date >= sixtyDaysAgo && s.Date < thirtyDaysAgo)
                .Select(s => s.E1rm).DefaultIfEmpty(0).Max();
            if (prevBest > 0)
                e1rmChangePct = Math.Round((topExercise.BestE1rm - prevBest) / prevBest * 100m, 1);
        }

        var strength = new StrengthSnapshotResponse(
            exerciseBests.Count, topExercise?.BestE1rm, topExercise?.Name, e1rmChangePct);

        // Recent workouts
        var recentWorkouts = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .Where(w => w.UserId == id)
            .OrderByDescending(w => w.ScheduledAt)
            .Take(10)
            .Select(w => new WorkoutSummaryResponse(w.Id, w.Name, w.ScheduledAt, w.Status.ToString(), w.Sets.Count))
            .ToListAsync(ct);

        // Alerts
        var alerts = await alertService.GetAlertsForAthleteAsync(currentUser.UserId, id, ct);
        var alertResponses = alerts.Select(a => new CoachAlertResponse(
            a.AthleteId, a.AthleteName, a.Category.ToString(), a.Severity, a.Message, a.Data)).ToList();

        // Deficit
        var activeDeficit = await db.DeficitPhases
            .AsNoTracking()
            .Where(d => d.UserId == id && d.Status == DeficitPhaseStatus.Active)
            .FirstOrDefaultAsync(ct);

        // Goals
        var goals = await db.Goals
            .AsNoTracking()
            .Include(g => g.Checkpoints)
            .Where(g => g.UserId == id && g.Status == GoalStatus.Active)
            .ToListAsync(ct);

        var goalSummaries = goals.Select(g =>
        {
            var currentValue = g.Checkpoints.OrderByDescending(c => c.Date).FirstOrDefault()?.Value ?? g.StartValue;
            return new GoalSummaryResponse(g.Id, g.Title, g.Type.ToString(), g.Status.ToString(),
                g.TargetValue, currentValue, g.TargetDate?.ToString("yyyy-MM-dd"));
        }).ToList();

        return Results.Ok(new CoachAthleteDashboardResponse(
            id, athlete.DisplayName, fatigue, volume, strength,
            recentWorkouts, alertResponses, activeDeficit?.Status.ToString(), goalSummaries));
    }

    private static async Task<IResult> GetAthleteWorkouts(
        Guid id, string? from, string? to,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view athlete workouts.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var (fromDate, toDate) = ParseDateRange(from, to);

        var workouts = await db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .Where(w => w.UserId == id
                && DateOnly.FromDateTime(w.ScheduledAt.UtcDateTime) >= fromDate
                && DateOnly.FromDateTime(w.ScheduledAt.UtcDateTime) <= toDate)
            .OrderByDescending(w => w.ScheduledAt)
            .Select(w => new WorkoutSummaryResponse(w.Id, w.Name, w.ScheduledAt, w.Status.ToString(), w.Sets.Count))
            .ToListAsync(ct);

        return Results.Ok(new WorkoutListResponse(workouts, workouts.Count));
    }

    private static async Task<IResult> GetAthleteChart(
        Guid id, string type, string? from, string? to, Guid? exerciseId,
        ICurrentUserService currentUser,
        ICoachAthleteService coachService,
        TrainingAppDbContext db,
        CancellationToken ct)
    {
        if (!currentUser.IsCoach)
            throw new ForbiddenException("Only coaches can view athlete charts.");

        await coachService.ValidateCoachAccessAsync(currentUser.UserId, id, ct);

        var (fromDate, toDate) = ParseDateRange(from, to);

        return type.ToLowerInvariant() switch
        {
            "strength" => await GetStrengthChart(id, exerciseId, fromDate, toDate, db, ct),
            "fatigue" => await GetFatigueChart(id, fromDate, toDate, db, ct),
            "volume" => await GetVolumeChart(id, fromDate, toDate, db, ct),
            _ => Results.BadRequest(new { error = $"Unknown chart type: {type}" })
        };
    }

    private static async Task<IResult> GetStrengthChart(Guid userId, Guid? exerciseId,
        DateOnly fromDate, DateOnly toDate, TrainingAppDbContext db, CancellationToken ct)
    {
        var query = db.WorkoutSets.AsNoTracking().Include(s => s.Workout)
            .Where(s => s.Workout!.UserId == userId && !s.IsWarmup
                && s.ActualWeight != null && s.ActualReps != null
                && s.ActualReps >= 1 && s.ActualReps <= 30
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= fromDate
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) <= toDate);

        if (exerciseId.HasValue)
            query = query.Where(s => s.ExerciseId == exerciseId.Value);

        var points = await query
            .Select(s => new
            {
                Date = DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime),
                E1rm = s.ActualWeight!.Value * (1 + s.ActualReps!.Value / 30m)
            })
            .ToListAsync(ct);

        var dailyBest = points.GroupBy(p => p.Date)
            .Select(g => (g.Key, g.Max(p => p.E1rm)))
            .OrderBy(p => p.Key).ToList();

        var chart = ChartDataService.BuildStrengthProgressionChart(dailyBest);
        return Results.Ok(ToChartDataResponse(chart));
    }

    private static async Task<IResult> GetFatigueChart(Guid userId,
        DateOnly fromDate, DateOnly toDate, TrainingAppDbContext db, CancellationToken ct)
    {
        var metrics = await db.DailyMetrics.AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= fromDate && m.Date <= toDate)
            .OrderBy(m => m.Date)
            .Select(m => new { m.Date, m.Ctl, m.Atl, m.Tsb })
            .ToListAsync(ct);

        var points = metrics.Select(m => (m.Date, m.Ctl, m.Atl, m.Tsb));
        var chart = ChartDataService.BuildFatigueChart(points);
        return Results.Ok(ToMultiSeriesResponse(chart));
    }

    private static async Task<IResult> GetVolumeChart(Guid userId,
        DateOnly fromDate, DateOnly toDate, TrainingAppDbContext db, CancellationToken ct)
    {
        var sets = await db.WorkoutSets.AsNoTracking().Include(s => s.Workout)
            .Where(s => s.Workout!.UserId == userId && !s.IsWarmup
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= fromDate
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) <= toDate)
            .Select(s => new
            {
                Date = DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime),
                Volume = (s.ActualWeight ?? 0) * (s.ActualReps ?? 0)
            })
            .ToListAsync(ct);

        var dailyData = sets.GroupBy(s => s.Date)
            .Select(g => (g.Key, g.Sum(s => s.Volume), g.Count()))
            .OrderBy(p => p.Key).ToList();

        var chart = ChartDataService.BuildVolumeChart(dailyData);
        return Results.Ok(ToMultiSeriesResponse(chart));
    }

    private static (DateOnly From, DateOnly To) ParseDateRange(string? from, string? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = from is not null && DateOnly.TryParseExact(from, "yyyy-MM-dd", out var f) ? f : today.AddDays(-30);
        var toDate = to is not null && DateOnly.TryParseExact(to, "yyyy-MM-dd", out var t) ? t : today;
        return (fromDate, toDate);
    }

    private static ChartDataResponse ToChartDataResponse(ChartDataService.ChartData chart) =>
        new(new ChartSeriesResponse(chart.Series.Name, chart.Series.Unit,
            chart.Series.Points.Select(p => new ChartPointResponse(p.Date.ToString("yyyy-MM-dd"), p.Value, p.Label)).ToList()),
            new ChartStatsResponse(chart.Statistics.Min, chart.Statistics.Max, chart.Statistics.Average,
                chart.Statistics.Current, chart.Statistics.ChangePercent));

    private static MultiSeriesChartResponse ToMultiSeriesResponse(ChartDataService.MultiSeriesChartData chart) =>
        new(chart.Series.Select(s => new ChartSeriesResponse(s.Name, s.Unit,
            s.Points.Select(p => new ChartPointResponse(p.Date.ToString("yyyy-MM-dd"), p.Value, p.Label)).ToList())).ToList(),
            chart.Title);
}
