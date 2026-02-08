using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class InsightGeneratorService : IInsightGeneratorService
{
    private readonly TrainingAppDbContext _db;

    public InsightGeneratorService(TrainingAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Insight>> GenerateInsightsAsync(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = today.AddDays(-30);
        var sixtyDaysAgo = today.AddDays(-60);
        var fourteenDaysAgo = today.AddDays(-14);

        var allResults = new List<InsightRuleEngine.InsightResult>();

        // ── Strength Insights ──
        var sets = await _db.WorkoutSets
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
                s.Exercise!.Name,
                s.ExerciseId,
                E1rm = s.ActualWeight!.Value * (1 + s.ActualReps!.Value / 30m)
            })
            .ToListAsync(ct);

        var exerciseData = sets
            .GroupBy(s => new { s.ExerciseId, s.Name })
            .Select(g =>
            {
                var currentBest = g.Where(s => s.Date >= thirtyDaysAgo).Select(s => s.E1rm).DefaultIfEmpty(0).Max();
                var prevBest = g.Where(s => s.Date >= sixtyDaysAgo && s.Date < thirtyDaysAgo).Select(s => s.E1rm).DefaultIfEmpty(0).Max();
                var allTimeBest = g.Select(s => s.E1rm).Max();
                var sessions30d = g.Where(s => s.Date >= thirtyDaysAgo).Select(s => s.Date).Distinct().Count();
                return (g.Key.Name, g.Key.ExerciseId, currentBest, prevBest, allTimeBest, sessions30d);
            })
            .ToList();

        allResults.AddRange(InsightRuleEngine.EvaluateStrengthInsights(exerciseData));

        // ── Volume Insights ──
        var sevenDaysAgo = today.AddDays(-7);
        var fourWeeksAgo = today.AddDays(-28);

        var dailyMetrics = await _db.DailyMetrics
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= fourWeeksAgo)
            .ToListAsync(ct);

        var currentWeekVolume = dailyMetrics.Where(m => m.Date >= sevenDaysAgo).Sum(m => m.TotalVolume);
        var prevWeekVolume = dailyMetrics.Where(m => m.Date >= sevenDaysAgo.AddDays(-7) && m.Date < sevenDaysAgo).Sum(m => m.TotalVolume);

        var workoutDays = dailyMetrics.Where(m => m.WorkoutCount > 0).Select(m => m.Date).Distinct().Count();
        var weeksSpan = Math.Max(1, (today.DayNumber - fourWeeksAgo.DayNumber) / 7);
        var sessionsPerWeek = workoutDays / weeksSpan;

        // Simplified muscle group data (using empty list since tracking per-muscle requires exercise mapping)
        allResults.AddRange(InsightRuleEngine.EvaluateVolumeInsights(
            currentWeekVolume, prevWeekVolume,
            Enumerable.Empty<(string, int, int)>(),
            sessionsPerWeek));

        // ── Recovery Insights ──
        var recentMetrics = dailyMetrics
            .Where(m => m.Date >= sevenDaysAgo)
            .Select(m => (m.Date, m.Tsb, m.ReadinessScore))
            .ToList();

        allResults.AddRange(InsightRuleEngine.EvaluateRecoveryInsights(recentMetrics));

        // ── Weight Insights ──
        var latestWeight = await _db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.Date)
            .FirstOrDefaultAsync(ct);

        var weight14dAgo = await _db.WeightLogs
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Date <= fourteenDaysAgo)
            .OrderByDescending(w => w.Date)
            .FirstOrDefaultAsync(ct);

        var activeDeficit = await _db.DeficitPhases
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Status == DeficitPhaseStatus.Active)
            .FirstOrDefaultAsync(ct);

        decimal? weightChange14d = null;
        if (latestWeight is not null && weight14dAgo is not null)
            weightChange14d = latestWeight.WeightKg - weight14dAgo.WeightKg;

        allResults.AddRange(InsightRuleEngine.EvaluateWeightInsights(
            latestWeight?.WeightKg,
            activeDeficit?.TargetWeightKg,
            weightChange14d,
            activeDeficit is not null,
            latestWeight?.WeeklyRateKg,
            activeDeficit?.WeeklyRateKg));

        // ── Deduplicate & Save ──
        var existingInsights = await _db.Insights
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.GeneratedDate == today)
            .Select(i => new { i.RuleName, i.RelatedEntityId })
            .ToListAsync(ct);

        var existingKeys = existingInsights
            .Select(i => $"{i.RuleName}:{i.RelatedEntityId}")
            .ToHashSet();

        var newInsights = new List<Insight>();
        foreach (var result in allResults)
        {
            var key = $"{result.RuleName}:{result.RelatedEntityId}";
            if (existingKeys.Contains(key))
                continue;

            var insight = new Insight
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RuleName = result.RuleName,
                Category = result.Category,
                Severity = result.Severity,
                Title = result.Title,
                Message = result.Message,
                GeneratedDate = today,
                RelatedEntityType = result.RelatedEntityType,
                RelatedEntityId = result.RelatedEntityId
            };

            _db.Insights.Add(insight);
            newInsights.Add(insight);
            existingKeys.Add(key);
        }

        if (newInsights.Count > 0)
            await _db.SaveChangesAsync(ct);

        return newInsights;
    }

    public async Task<List<Insight>> GetInsightHistoryAsync(Guid userId, DateOnly from, DateOnly to, string? category, CancellationToken ct)
    {
        var query = _db.Insights
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.GeneratedDate >= from && i.GeneratedDate <= to);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(i => i.Category == category);

        return await query
            .OrderByDescending(i => i.GeneratedDate)
            .ThenByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
    }
}
