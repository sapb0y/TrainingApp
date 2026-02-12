using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class UserDashboardService : IUserDashboardService
{
    private readonly TrainingAppDbContext _db;

    public UserDashboardService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<UserDashboardData> GetDashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = today.AddDays(-30);
        var sevenDaysAgo = today.AddDays(-7);
        var sixtyDaysAgo = today.AddDays(-60);

        // ── Strength Snapshot ──
        var recentSets = await _db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Workout)
            .Include(s => s.Exercise)
            .Where(s => s.Workout!.UserId == userId
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
                .Select(s => s.E1rm)
                .DefaultIfEmpty(0)
                .Max();

            if (prevBest > 0)
                e1rmChangePct = Math.Round((topExercise.BestE1rm - prevBest) / prevBest * 100m, 1);
        }

        var strengthSnapshot = new ChartDataService.StrengthSnapshot(
            exerciseBests.Count, topExercise?.BestE1rm, topExercise?.Name, e1rmChangePct);

        // ── Body Weight Snapshot ──
        var latestWeight = await _db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.Date)
            .FirstOrDefaultAsync(ct);

        var weightTrend = "N/A";
        if (latestWeight is not null)
        {
            var weightPoints = await _db.WeightLogs.AsNoTracking()
                .Where(w => w.UserId == userId && w.Date >= thirtyDaysAgo)
                .OrderBy(w => w.Date)
                .Select(w => new { w.Date, w.WeightKg })
                .ToListAsync(ct);

            weightTrend = ProgressAnalyticsService.DetectTrend(
                weightPoints.Select(w => (w.Date, w.WeightKg)));
        }

        var bodyWeightSnapshot = new ChartDataService.BodyWeightSnapshot(
            latestWeight?.WeightKg, latestWeight?.MovingAverage7d,
            latestWeight?.WeeklyRateKg, weightTrend);

        // ── Cardio Snapshot ──
        var cardioSessions = await _db.CardioSessions
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Date >= thirtyDaysAgo)
            .ToListAsync(ct);

        var cardioSnapshot = new ChartDataService.CardioSnapshot(
            cardioSessions.Count,
            cardioSessions.Sum(c => c.DurationMinutes),
            cardioSessions.Where(c => c.DistanceKm.HasValue).Sum(c => c.DistanceKm),
            cardioSessions.Count > 0
                ? Math.Round(cardioSessions.Where(c => c.CardioTrimp.HasValue).Select(c => c.CardioTrimp!.Value).DefaultIfEmpty(0).Average(), 1)
                : null);

        // ── Fatigue Snapshot ──
        var latestMetrics = await _db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == userId)
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

        var fatigueSnapshot = new ChartDataService.FatigueSnapshot(
            latestMetrics?.Ctl, latestMetrics?.Atl, latestMetrics?.Tsb, readinessCategory);

        // ── Volume Snapshot ──
        var last7dMetrics = await _db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= sevenDaysAgo)
            .ToListAsync(ct);

        var prev7dMetrics = await _db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= sevenDaysAgo.AddDays(-7) && m.Date < sevenDaysAgo)
            .ToListAsync(ct);

        var totalSets7d = last7dMetrics.Sum(m => m.TotalSets);
        var totalVolume7d = last7dMetrics.Sum(m => m.TotalVolume);
        var prevVolume7d = prev7dMetrics.Sum(m => m.TotalVolume);

        decimal? volumeChangePct = null;
        if (prevVolume7d > 0)
            volumeChangePct = Math.Round((totalVolume7d - prevVolume7d) / prevVolume7d * 100m, 1);

        var volumeSnapshot = new ChartDataService.VolumeSnapshot(
            totalSets7d, totalVolume7d > 0 ? totalVolume7d : null, volumeChangePct);

        // ── Active Days ──
        var workoutDates = await _db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Workout)
            .Where(s => s.Workout!.UserId == userId
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= thirtyDaysAgo)
            .Select(s => DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime))
            .Distinct()
            .ToListAsync(ct);

        var cardioDates = await _db.CardioSessions
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Date >= thirtyDaysAgo)
            .Select(c => c.Date)
            .Distinct()
            .ToListAsync(ct);

        var activeDays = workoutDates.Union(cardioDates).Distinct().Count();

        // ── Personal Records ──
        var allHistory = recentSets.Select(s => (s.Date, s.E1rm, s.ExerciseName));
        var recentPRs = ProgressAnalyticsService.FindPersonalRecords(allHistory)
            .Where(pr => pr.IsRecent)
            .Take(5)
            .Select(pr => new UserDashboardPR(pr.ExerciseName, Math.Round(pr.BestE1rm, 1), pr.Date))
            .ToList();

        // ── Assemble ──
        var summary = ChartDataService.CalculateDashboardSummary(
            strengthSnapshot, bodyWeightSnapshot, cardioSnapshot, fatigueSnapshot, volumeSnapshot, activeDays);

        return new UserDashboardData(summary, activeDays, recentPRs);
    }
}
