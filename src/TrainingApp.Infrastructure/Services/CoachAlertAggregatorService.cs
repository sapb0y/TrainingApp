using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class CoachAlertAggregatorService : ICoachAlertAggregatorService
{
    private readonly TrainingAppDbContext _db;
    private readonly ICoachAthleteService _coachAthleteService;

    public CoachAlertAggregatorService(TrainingAppDbContext db, ICoachAthleteService coachAthleteService)
    {
        _db = db;
        _coachAthleteService = coachAthleteService;
    }

    public async Task<List<CoachAlertEngine.CoachAlert>> GetAlertsForAllAthletesAsync(Guid coachId, CancellationToken ct)
    {
        var athletes = await _coachAthleteService.GetAthletesAsync(coachId, ct);
        var allAlerts = new List<CoachAlertEngine.CoachAlert>();

        foreach (var rel in athletes)
        {
            if (rel.AthleteId is null) continue;
            var alerts = await GetAlertsForAthleteInternalAsync(rel.AthleteId.Value, rel.Athlete?.DisplayName ?? "Unknown", ct);
            allAlerts.AddRange(alerts);
        }

        return CoachAlertEngine.SortBySeverity(allAlerts);
    }

    public async Task<List<CoachAlertEngine.CoachAlert>> GetAlertsForAthleteAsync(Guid coachId, Guid athleteId, CancellationToken ct)
    {
        await _coachAthleteService.ValidateCoachAccessAsync(coachId, athleteId, ct);

        var athlete = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == athleteId, ct);
        var name = athlete?.DisplayName ?? "Unknown";

        var alerts = await GetAlertsForAthleteInternalAsync(athleteId, name, ct);
        return CoachAlertEngine.SortBySeverity(alerts);
    }

    private async Task<List<CoachAlertEngine.CoachAlert>> GetAlertsForAthleteInternalAsync(
        Guid athleteId, string athleteName, CancellationToken ct)
    {
        var alerts = new List<CoachAlertEngine.CoachAlert>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = today.AddDays(-7);

        // 1. Fatigue: TSB + readiness
        var latestMetrics = await _db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == athleteId)
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync(ct);

        decimal? readinessPct = latestMetrics?.ReadinessScore.HasValue == true
            ? latestMetrics.ReadinessScore.Value * 10m // scale 0-10 to 0-100
            : null;

        var fatigueAlert = CoachAlertEngine.EvaluateFatigue(athleteId, athleteName,
            latestMetrics?.Tsb, readinessPct);
        if (fatigueAlert is not null) alerts.Add(fatigueAlert);

        // 2. Overreaching: avg session RPE last 3 workouts
        var recentRpes = await _db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == athleteId && w.Status == WorkoutStatus.Completed && w.SessionRpe != null)
            .OrderByDescending(w => w.CompletedAt)
            .Take(3)
            .Select(w => w.SessionRpe!.Value)
            .ToListAsync(ct);

        if (recentRpes.Count >= 3)
        {
            var avgRpe = recentRpes.Average();
            var overreachAlert = CoachAlertEngine.EvaluateOverreaching(athleteId, athleteName, (decimal)avgRpe);
            if (overreachAlert is not null) alerts.Add(overreachAlert);
        }

        // 3. Missed sessions (7d)
        var missedCount = await _db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == athleteId
                && w.Status == WorkoutStatus.Skipped
                && DateOnly.FromDateTime(w.ScheduledAt.UtcDateTime) >= sevenDaysAgo)
            .CountAsync(ct);

        var missedAlert = CoachAlertEngine.EvaluateMissedSessions(athleteId, athleteName, missedCount);
        if (missedAlert is not null) alerts.Add(missedAlert);

        // 4. RPE Drift: avg RIR deviation last 7d
        var recentSets = await _db.WorkoutSets
            .AsNoTracking()
            .Include(s => s.Workout)
            .Where(s => s.Workout!.UserId == athleteId
                && s.Rir.HasValue && s.TargetRir.HasValue
                && s.Workout!.CompletedAt != null
                && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= sevenDaysAgo)
            .Select(s => s.Rir!.Value - s.TargetRir!.Value)
            .ToListAsync(ct);

        if (recentSets.Count > 0)
        {
            var avgDrift = (decimal)recentSets.Average();
            var driftAlert = CoachAlertEngine.EvaluateRpeDrift(athleteId, athleteName, avgDrift);
            if (driftAlert is not null) alerts.Add(driftAlert);
        }

        // 5. Deficit stress: active deficit + strength declining >5%
        var activeDeficit = await _db.DeficitPhases
            .AsNoTracking()
            .Where(d => d.UserId == athleteId && d.Status == DeficitPhaseStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (activeDeficit is not null)
        {
            var thirtyDaysAgo = today.AddDays(-30);
            var sixtyDaysAgo = today.AddDays(-60);

            var e1rmData = await _db.WorkoutSets
                .AsNoTracking()
                .Include(s => s.Workout)
                .Where(s => s.Workout!.UserId == athleteId
                    && !s.IsWarmup
                    && s.ActualWeight != null && s.ActualReps != null
                    && s.ActualReps >= 1 && s.ActualReps <= 30
                    && s.Workout!.CompletedAt != null
                    && DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime) >= sixtyDaysAgo)
                .Select(s => new
                {
                    Date = DateOnly.FromDateTime(s.Workout!.CompletedAt!.Value.UtcDateTime),
                    ExerciseId = s.ExerciseId,
                    E1rm = s.ActualWeight!.Value * (1 + s.ActualReps!.Value / 30m)
                })
                .ToListAsync(ct);

            var recentBest = e1rmData.Where(s => s.Date >= thirtyDaysAgo)
                .GroupBy(s => s.ExerciseId)
                .ToDictionary(g => g.Key, g => g.Max(s => s.E1rm));

            var prevBest = e1rmData.Where(s => s.Date >= sixtyDaysAgo && s.Date < thirtyDaysAgo)
                .GroupBy(s => s.ExerciseId)
                .ToDictionary(g => g.Key, g => g.Max(s => s.E1rm));

            var changes = new List<decimal>();
            foreach (var (exerciseId, recent) in recentBest)
            {
                if (prevBest.TryGetValue(exerciseId, out var prev) && prev > 0)
                    changes.Add((recent - prev) / prev * 100m);
            }

            if (changes.Count > 0)
            {
                var avgChange = changes.Average();
                var deficitAlert = CoachAlertEngine.EvaluateDeficitStress(athleteId, athleteName, true, avgChange);
                if (deficitAlert is not null) alerts.Add(deficitAlert);
            }
        }

        // 6. Stalled goals
        var activeGoals = await _db.Goals
            .AsNoTracking()
            .Include(g => g.Checkpoints)
            .Where(g => g.UserId == athleteId && g.Status == GoalStatus.Active && g.TargetDate.HasValue)
            .ToListAsync(ct);

        foreach (var goal in activeGoals)
        {
            var currentValue = goal.Checkpoints.OrderByDescending(c => c.Date).FirstOrDefault()?.Value ?? goal.StartValue ?? 0;
            decimal percentComplete = 0;
            if (goal.TargetValue.HasValue && goal.StartValue.HasValue && goal.TargetValue != goal.StartValue)
                percentComplete = Math.Abs((currentValue - goal.StartValue.Value) / (goal.TargetValue.Value - goal.StartValue.Value) * 100m);

            var goalAlert = CoachAlertEngine.EvaluateGoalStalled(athleteId, athleteName, goal.Title, percentComplete, goal.TargetDate);
            if (goalAlert is not null) alerts.Add(goalAlert);
        }

        return alerts;
    }
}
