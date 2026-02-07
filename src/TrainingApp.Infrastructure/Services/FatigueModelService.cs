using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class FatigueModelService : IFatigueModelService
{
    private readonly TrainingAppDbContext _db;
    private readonly ILogger<FatigueModelService> _logger;

    public FatigueModelService(TrainingAppDbContext db, ILogger<FatigueModelService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task UpdateDailyMetricsAsync(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        // Get previous day's metrics for CTL/ATL carry-forward
        var prevDate = date.AddDays(-1);
        var prev = await _db.DailyMetrics
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Date == prevDate)
            .FirstOrDefaultAsync(ct);

        var prevCtl = prev?.Ctl ?? 0m;
        var prevAtl = prev?.Atl ?? 0m;

        // Get completed workouts for this date
        var workouts = await _db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .Where(w => w.UserId == userId
                && w.Status == WorkoutStatus.Completed
                && w.CompletedAt.HasValue
                && DateOnly.FromDateTime(w.CompletedAt.Value.UtcDateTime) == date)
            .ToListAsync(ct);

        // Calculate session stress for each workout
        var sessionStresses = new List<TrainingStressService.SessionStress>();
        var sessionRpes = new List<int?>();

        foreach (var workout in workouts)
        {
            var sets = workout.Sets.Select(s =>
                (s.ActualWeight, s.ActualReps, s.Rpe, s.Rir, s.IsWarmup));
            var sessionStress = TrainingStressService.CalculateSessionStress(sets);
            sessionStresses.Add(sessionStress);
            sessionRpes.Add(workout.SessionRpe);
        }

        // Aggregate daily stress
        var dailyStress = TrainingStressService.CalculateDailyStress(sessionStresses, sessionRpes);

        // Update fatigue state
        var fatigueState = TrainingStressService.UpdateFatigueState(prevCtl, prevAtl, dailyStress.Trimp);

        // Get recovery log for readiness calculation
        var recoveryLog = await _db.RecoveryLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Date == date, ct);

        // Get user's recovery capacity
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var capacity = user?.Settings.RecoveryCapacity ?? RecoveryCapacity.Normal;

        // Calculate readiness
        var readiness = TrainingStressService.CalculateReadiness(
            fatigueState.Tsb,
            capacity,
            recoveryLog?.SleepQuality,
            recoveryLog?.StressLevel,
            recoveryLog?.EnergyLevel,
            recoveryLog?.MuscleReadiness,
            recoveryLog?.Mood);

        // Evaluate fatigue rules
        var daysSinceDeload = await GetDaysSinceDeloadAsync(userId, date, ct);
        var daysSinceWorkout = await GetDaysSinceLastWorkoutAsync(userId, date, ct);
        var alerts = FatigueRuleEngine.EvaluateRules(fatigueState.Tsb, readiness.Score, daysSinceDeload, daysSinceWorkout);

        // Write adaptation logs for triggered rules
        foreach (var alert in alerts)
        {
            // Find a workout to attach the log to (use the last completed workout for this date, or any recent)
            var workoutId = workouts.LastOrDefault()?.Id
                ?? await _db.Workouts
                    .Where(w => w.UserId == userId && w.Status == WorkoutStatus.Completed)
                    .OrderByDescending(w => w.CompletedAt)
                    .Select(w => (Guid?)w.Id)
                    .FirstOrDefaultAsync(ct);

            if (workoutId.HasValue)
            {
                _db.AdaptationLogs.Add(new AdaptationLog
                {
                    Id = Guid.NewGuid(),
                    WorkoutId = workoutId.Value,
                    RuleName = alert.RuleName,
                    Scope = alert.Scope,
                    InputSummary = alert.InputSummary,
                    OutputSummary = alert.OutputSummary,
                    WasApplied = false
                });
            }
        }

        // Upsert daily metrics
        var existing = await _db.DailyMetrics
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, ct);

        if (existing is not null)
        {
            existing.Trimp = dailyStress.Trimp;
            existing.Ctl = fatigueState.Ctl;
            existing.Atl = fatigueState.Atl;
            existing.Tsb = fatigueState.Tsb;
            existing.TotalSets = dailyStress.TotalSets;
            existing.TotalReps = dailyStress.TotalReps;
            existing.TotalVolume = dailyStress.TotalVolume;
            existing.WorkoutCount = dailyStress.WorkoutCount;
            existing.AverageSessionRpe = dailyStress.AverageSessionRpe;
            existing.ReadinessScore = readiness.Score;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            _db.DailyMetrics.Add(new DailyMetrics
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                Trimp = dailyStress.Trimp,
                Ctl = fatigueState.Ctl,
                Atl = fatigueState.Atl,
                Tsb = fatigueState.Tsb,
                TotalSets = dailyStress.TotalSets,
                TotalReps = dailyStress.TotalReps,
                TotalVolume = dailyStress.TotalVolume,
                WorkoutCount = dailyStress.WorkoutCount,
                AverageSessionRpe = dailyStress.AverageSessionRpe,
                ReadinessScore = readiness.Score
            });
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated daily metrics for user {UserId} on {Date}: TRIMP={Trimp:F2}, CTL={Ctl:F2}, ATL={Atl:F2}, TSB={Tsb:F2}",
            userId, date, dailyStress.Trimp, fatigueState.Ctl, fatigueState.Atl, fatigueState.Tsb);
    }

    public async Task<DailyMetrics?> GetReadinessAsync(Guid userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var metrics = await _db.DailyMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == today, ct);

        if (metrics is null)
        {
            // Calculate if not yet computed today
            await UpdateDailyMetricsAsync(userId, today, ct);
            metrics = await _db.DailyMetrics
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == today, ct);
        }

        return metrics;
    }

    public async Task<List<DailyMetrics>> GetHistoryAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await _db.DailyMetrics
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= from && d.Date <= to)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);
    }

    public async Task<RecoveryLog> LogRecoveryAsync(Guid userId, DateOnly date, int? sleepQuality, decimal? sleepHours,
        int? stressLevel, int? energyLevel, int? muscleReadiness, int? mood, string? notes, CancellationToken ct = default)
    {
        var existing = await _db.RecoveryLogs
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Date == date, ct);

        if (existing is not null)
        {
            existing.SleepQuality = sleepQuality;
            existing.SleepHours = sleepHours;
            existing.StressLevel = stressLevel;
            existing.EnergyLevel = energyLevel;
            existing.MuscleReadiness = muscleReadiness;
            existing.Mood = mood;
            existing.Notes = notes;
        }
        else
        {
            existing = new RecoveryLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                SleepQuality = sleepQuality,
                SleepHours = sleepHours,
                StressLevel = stressLevel,
                EnergyLevel = energyLevel,
                MuscleReadiness = muscleReadiness,
                Mood = mood,
                Notes = notes
            };
            _db.RecoveryLogs.Add(existing);
        }

        await _db.SaveChangesAsync(ct);

        // Recalculate readiness if daily metrics exist for this date
        var metricsExist = await _db.DailyMetrics.AnyAsync(d => d.UserId == userId && d.Date == date, ct);
        if (metricsExist)
        {
            await UpdateDailyMetricsAsync(userId, date, ct);
        }

        return existing;
    }

    public async Task<List<RecoveryLog>> GetRecoveryLogsAsync(Guid userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        return await _db.RecoveryLogs
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Date >= from && r.Date <= to)
            .OrderBy(r => r.Date)
            .ToListAsync(ct);
    }

    public async Task RecalculateAsync(Guid userId, DateOnly? from, CancellationToken ct = default)
    {
        var startDate = from ?? await _db.Workouts
            .Where(w => w.UserId == userId && w.Status == WorkoutStatus.Completed && w.CompletedAt.HasValue)
            .OrderBy(w => w.CompletedAt)
            .Select(w => DateOnly.FromDateTime(w.CompletedAt!.Value.UtcDateTime))
            .FirstOrDefaultAsync(ct);

        if (startDate == default)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Delete existing metrics in range to rebuild
        var existingMetrics = await _db.DailyMetrics
            .Where(d => d.UserId == userId && d.Date >= startDate && d.Date <= today)
            .ToListAsync(ct);
        _db.DailyMetrics.RemoveRange(existingMetrics);
        await _db.SaveChangesAsync(ct);

        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            await UpdateDailyMetricsAsync(userId, date, ct);
        }

        _logger.LogInformation("Recalculated daily metrics for user {UserId} from {From} to {To}", userId, startDate, today);
    }

    private async Task<int> GetDaysSinceDeloadAsync(Guid userId, DateOnly date, CancellationToken ct)
    {
        // A deload is approximated by a week where TRIMP was < 50% of recent average
        // For simplicity, look for any week in last 42 days with significantly lower load
        var lookback = date.AddDays(-42);
        var weeklyTrimp = await _db.DailyMetrics
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Date >= lookback && d.Date < date)
            .OrderByDescending(d => d.Date)
            .Select(d => new { d.Date, d.Trimp })
            .ToListAsync(ct);

        if (weeklyTrimp.Count < 7)
            return weeklyTrimp.Count > 0 ? (date.DayNumber - weeklyTrimp.Min(w => w.Date).DayNumber) : 0;

        var avgTrimp = weeklyTrimp.Average(w => w.Trimp);
        if (avgTrimp <= 0) return 0;

        // Check 7-day windows for deload-like periods
        for (var i = 0; i <= weeklyTrimp.Count - 7; i++)
        {
            var window = weeklyTrimp.Skip(i).Take(7).ToList();
            var windowAvg = window.Average(w => w.Trimp);
            if (windowAvg < avgTrimp * 0.5m)
                return (date.DayNumber - window.First().Date.DayNumber);
        }

        return weeklyTrimp.Count;
    }

    private async Task<int> GetDaysSinceLastWorkoutAsync(Guid userId, DateOnly date, CancellationToken ct)
    {
        var lastWorkout = await _db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Status == WorkoutStatus.Completed && w.CompletedAt.HasValue)
            .OrderByDescending(w => w.CompletedAt)
            .FirstOrDefaultAsync(ct);

        if (lastWorkout?.CompletedAt is null)
            return 0;

        var lastDate = DateOnly.FromDateTime(lastWorkout.CompletedAt.Value.UtcDateTime);
        return Math.Max(0, date.DayNumber - lastDate.DayNumber);
    }
}
