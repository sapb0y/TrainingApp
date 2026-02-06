using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Core.Services;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class AutoregulationExecutionService : IAutoregulationExecutionService
{
    private readonly TrainingAppDbContext _db;
    private readonly ILogger<AutoregulationExecutionService> _logger;

    public AutoregulationExecutionService(TrainingAppDbContext db, ILogger<AutoregulationExecutionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AutoregulationService.WorkoutEvaluation> GetRecommendationsAsync(Guid workoutId, CancellationToken ct = default)
    {
        var workout = await _db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .ThenInclude(s => s.Exercise)
            .FirstOrDefaultAsync(w => w.Id == workoutId, ct)
            ?? throw new Core.Exceptions.NotFoundException("Workout", workoutId.ToString());

        var exerciseGroups = workout.Sets
            .Where(s => !s.IsWarmup && s.CompletedAt.HasValue)
            .GroupBy(s => s.ExerciseId)
            .ToList();

        var recommendations = new List<AutoregulationService.ExerciseRecommendation>();
        const decimal defaultIncrement = 2.5m;

        foreach (var group in exerciseGroups)
        {
            var sets = group.OrderBy(s => s.SetNumber).ToList();
            var latestSet = sets.Last();
            var exerciseName = latestSet.Exercise?.Name ?? "Unknown";

            // Evaluate latest set for load adjustment
            var setEval = AutoregulationService.EvaluateSet(
                latestSet.TargetRir, latestSet.Rir, latestSet.ActualWeight ?? latestSet.TargetWeight ?? 0m, defaultIncrement);

            // Evaluate volume across all completed sets
            var rirPairs = sets.Select(s => (s.TargetRir, s.Rir)).ToList();
            var targetSets = workout.Sets.Count(s => s.ExerciseId == group.Key && !s.IsWarmup);
            var volumeAction = AutoregulationService.EvaluateExerciseVolume(rirPairs, targetSets);

            // Build next set prescription
            AutoregulationService.SetPrescription? nextSet = null;
            if (setEval.Load is not null)
            {
                nextSet = new AutoregulationService.SetPrescription(
                    setEval.Load.RecommendedWeight,
                    latestSet.TargetReps ?? latestSet.ActualReps ?? 8,
                    latestSet.TargetRir ?? 2);
            }

            // Build reason
            var reason = setEval.Reason ?? "No RIR data";
            if (volumeAction != AutoregulationService.VolumeAction.Continue)
                reason += $" | Volume: {volumeAction}";

            recommendations.Add(new AutoregulationService.ExerciseRecommendation(
                group.Key, volumeAction, nextSet, reason));

            // Write adaptation log if adjustment recommended
            if (setEval.Load is not null && setEval.Load.Type != AutoregulationService.AdjustmentType.Maintain)
            {
                _db.AdaptationLogs.Add(new AdaptationLog
                {
                    Id = Guid.NewGuid(),
                    WorkoutId = workoutId,
                    WorkoutSetId = latestSet.Id,
                    RuleName = setEval.Load.Type == AutoregulationService.AdjustmentType.Reduce ? "LoadReduction" : "LoadIncrease",
                    Scope = "IntraSet",
                    InputSummary = $"targetRir={latestSet.TargetRir}, actualRir={latestSet.Rir}, weight={latestSet.ActualWeight ?? latestSet.TargetWeight}, drift={latestSet.RirDrift}",
                    OutputSummary = $"recommendedWeight={setEval.Load.RecommendedWeight}, adjustmentPct={setEval.Load.AdjustmentPercent}%"
                });
            }

            if (volumeAction != AutoregulationService.VolumeAction.Continue)
            {
                _db.AdaptationLogs.Add(new AdaptationLog
                {
                    Id = Guid.NewGuid(),
                    WorkoutId = workoutId,
                    RuleName = volumeAction == AutoregulationService.VolumeAction.SkipRemaining ? "VolumeSkip" : "VolumeAdd",
                    Scope = "IntraWorkout",
                    InputSummary = $"completedSets={sets.Count}, targetSets={targetSets}, avgDrift={rirPairs.Where(p => p.TargetRir.HasValue && p.Rir.HasValue).Select(p => p.Rir!.Value - p.TargetRir!.Value).DefaultIfEmpty(0).Average():F1}",
                    OutputSummary = $"volumeAction={volumeAction}"
                });
            }
        }

        // Session-level evaluation
        var recentSessionRpes = await _db.Workouts
            .AsNoTracking()
            .Where(w => w.UserId == workout.UserId && w.Status == WorkoutStatus.Completed && w.SessionRpe.HasValue)
            .OrderByDescending(w => w.CompletedAt)
            .Take(5)
            .Select(w => w.SessionRpe)
            .ToListAsync(ct);

        var sessionNote = AutoregulationService.EvaluateSession(recentSessionRpes, workout.Sets.Count);

        if (sessionNote is not null)
        {
            _db.AdaptationLogs.Add(new AdaptationLog
            {
                Id = Guid.NewGuid(),
                WorkoutId = workoutId,
                RuleName = "DeloadSuggestion",
                Scope = "PostWorkout",
                InputSummary = $"recentSessionRpes=[{string.Join(",", recentSessionRpes)}]",
                OutputSummary = sessionNote
            });
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Generated {Count} recommendations for workout {WorkoutId}", recommendations.Count, workoutId);

        return new AutoregulationService.WorkoutEvaluation(recommendations, sessionNote);
    }

    public async Task<List<AdaptationLog>> GetAdaptationLogAsync(Guid workoutId, CancellationToken ct = default)
    {
        return await _db.AdaptationLogs
            .AsNoTracking()
            .Where(a => a.WorkoutId == workoutId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }
}
