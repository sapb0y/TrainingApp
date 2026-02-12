using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Infrastructure.Services;

public class SmartWorkoutService : ISmartWorkoutService
{
    private readonly TrainingAppDbContext _db;
    private readonly IFatigueModelService _fatigueService;
    private readonly IExerciseCacheService _exerciseCache;

    public SmartWorkoutService(
        TrainingAppDbContext db,
        IFatigueModelService fatigueService,
        IExerciseCacheService exerciseCache)
    {
        _db = db;
        _fatigueService = fatigueService;
        _exerciseCache = exerciseCache;
    }

    public async Task<List<WorkoutOption>> GenerateWorkoutOptionsAsync(
        Guid userId, int durationMinutes = 45, CancellationToken ct = default)
    {
        var readiness = await _fatigueService.GetReadinessAsync(userId, ct);
        var recentWorkouts = await _db.Workouts
            .AsNoTracking()
            .Include(w => w.Sets)
            .Where(w => w.UserId == userId && w.Status == WorkoutStatus.Completed)
            .OrderByDescending(w => w.CompletedAt)
            .Take(10)
            .ToListAsync(ct);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var goals = await _db.Goals.AsNoTracking()
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .ToListAsync(ct);

        var exercises = await _exerciseCache.GetAllExercisesAsync(ct);
        if (exercises.Count == 0) return [];

        var tsb = readiness?.Tsb ?? 0;
        var readinessScore = readiness?.ReadinessScore ?? 7;
        var isInDeficit = await _db.DeficitPhases.AsNoTracking()
            .AnyAsync(d => d.UserId == userId && d.Status == DeficitPhaseStatus.Active, ct);
        var hasStrengthGoal = goals.Any(g => g.Type == GoalType.Strength);

        // Pick candidate workout types based on readiness
        var candidates = GetCandidateTypes(tsb, readinessScore, isInDeficit, hasStrengthGoal);

        // Diversify: avoid recent workout types
        var recentTypes = InferRecentTypes(recentWorkouts);
        candidates = candidates
            .OrderByDescending(t => recentTypes.Contains(t) ? 0 : 1)
            .ThenBy(_ => Random.Shared.Next())
            .Take(3)
            .ToList();

        // Build 3 workout options
        var options = new List<WorkoutOption>();
        foreach (var type in candidates)
        {
            var option = BuildOption(type, durationMinutes, exercises, recentWorkouts, user?.Settings);
            options.Add(option);
        }

        return options;
    }

    private static List<WorkoutType> GetCandidateTypes(
        decimal tsb, decimal? readiness, bool inDeficit, bool hasStrengthGoal)
    {
        var all = new List<WorkoutType>();

        if (readiness < 4)
        {
            // Very fatigued — recovery only
            return [WorkoutType.Recovery, WorkoutType.Mobility, WorkoutType.Deload];
        }

        if (tsb < -10)
        {
            // Accumulated fatigue — no high-intensity
            all.AddRange([WorkoutType.Hypertrophy, WorkoutType.Endurance,
                WorkoutType.Recovery, WorkoutType.Mobility, WorkoutType.Deload]);
        }
        else
        {
            // Normal — full pool
            all.AddRange([WorkoutType.Strength, WorkoutType.Power, WorkoutType.Hypertrophy,
                WorkoutType.Endurance, WorkoutType.MetabolicConditioning,
                WorkoutType.HIIT, WorkoutType.Recovery, WorkoutType.Deload]);
        }

        // Bias based on context
        if (inDeficit)
        {
            all.Add(WorkoutType.MetabolicConditioning);
            all.Add(WorkoutType.Endurance);
        }

        if (hasStrengthGoal)
        {
            all.Add(WorkoutType.Strength);
            all.Add(WorkoutType.Power);
        }

        return all.Distinct().ToList();
    }

    private static List<WorkoutType> InferRecentTypes(List<Workout> recent)
    {
        var types = new List<WorkoutType>();
        foreach (var w in recent.Take(3))
        {
            var name = w.Name.ToLowerInvariant();
            if (name.Contains("strength")) types.Add(WorkoutType.Strength);
            else if (name.Contains("power")) types.Add(WorkoutType.Power);
            else if (name.Contains("hypertrophy")) types.Add(WorkoutType.Hypertrophy);
            else if (name.Contains("endurance")) types.Add(WorkoutType.Endurance);
            else if (name.Contains("hiit")) types.Add(WorkoutType.HIIT);
            else if (name.Contains("recovery") || name.Contains("deload"))
                types.Add(WorkoutType.Recovery);
        }
        return types;
    }

    private static WorkoutOption BuildOption(
        WorkoutType type, int durationMinutes,
        IReadOnlyList<Exercise> exercises, List<Workout> recent,
        UserSettings? settings)
    {
        var (sets, reps, restSec, intensity) = GetTypeParams(type);
        var exerciseCount = Math.Max(3, durationMinutes / (sets * (restSec / 60 + 1)));
        exerciseCount = Math.Min(exerciseCount, 8);

        var categories = GetCategoriesForType(type);
        var selectedExercises = exercises
            .Where(e => categories.Length == 0 || categories.Any(c =>
                e.Category.Contains(c, StringComparison.OrdinalIgnoreCase) ||
                e.PrimaryMuscles.Any(m => m.Contains(c, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(_ => Random.Shared.Next())
            .Take(exerciseCount)
            .ToList();

        // Fallback if not enough matching exercises
        if (selectedExercises.Count < 3)
        {
            selectedExercises = exercises
                .OrderBy(_ => Random.Shared.Next())
                .Take(exerciseCount)
                .ToList();
        }

        // Infer weights from recent workout history
        var recentSetsByExercise = recent
            .SelectMany(w => w.Sets)
            .Where(s => s.ActualWeight.HasValue && s.ActualWeight > 0)
            .GroupBy(s => s.ExerciseId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.CompletedAt).First().ActualWeight!.Value);

        var optionExercises = selectedExercises.Select(e =>
        {
            var weight = recentSetsByExercise.GetValueOrDefault(e.Id);
            var suggestedWeight = weight > 0 ? Math.Round(weight * intensity, 1) : (decimal?)null;
            return new WorkoutOptionExercise(e.Id, e.Name, sets, reps, suggestedWeight);
        }).ToList();

        return new WorkoutOption(
            Name: $"{type} ({durationMinutes}min)",
            Type: type,
            DurationMinutes: durationMinutes,
            Description: GetTypeDescription(type),
            Exercises: optionExercises
        );
    }

    private static (int sets, int reps, int restSec, decimal intensityFactor) GetTypeParams(WorkoutType type) => type switch
    {
        WorkoutType.Strength => (5, 3, 180, 0.90m),
        WorkoutType.Power => (4, 2, 180, 0.85m),
        WorkoutType.Hypertrophy => (4, 10, 90, 0.70m),
        WorkoutType.Endurance => (3, 15, 60, 0.55m),
        WorkoutType.MetabolicConditioning => (3, 12, 45, 0.60m),
        WorkoutType.Recovery => (2, 12, 60, 0.50m),
        WorkoutType.Mobility => (2, 10, 30, 0.30m),
        WorkoutType.HIIT => (4, 8, 30, 0.75m),
        WorkoutType.Deload => (3, 8, 90, 0.50m),
        _ => (3, 10, 90, 0.65m)
    };

    private static string[] GetCategoriesForType(WorkoutType type) => type switch
    {
        WorkoutType.Strength => ["Chest", "Back", "Legs", "Shoulders"],
        WorkoutType.Power => ["Legs", "Back", "Shoulders"],
        WorkoutType.Hypertrophy => ["Chest", "Back", "Legs", "Arms", "Shoulders"],
        WorkoutType.Endurance => ["Legs", "Cardio"],
        WorkoutType.MetabolicConditioning => ["Legs", "Chest", "Back"],
        WorkoutType.Recovery => ["Stretching", "Calves", "Abs"],
        WorkoutType.Mobility => ["Stretching", "Abs"],
        WorkoutType.HIIT => ["Legs", "Chest", "Back", "Shoulders"],
        WorkoutType.Deload => ["Chest", "Back", "Legs"],
        _ => []
    };

    private static string GetTypeDescription(WorkoutType type) => type switch
    {
        WorkoutType.Strength => "Heavy compound lifts, low reps, long rest. Build maximal strength.",
        WorkoutType.Power => "Explosive movements, very low reps. Develop speed and force.",
        WorkoutType.Hypertrophy => "Moderate weight, higher reps. Maximize muscle growth.",
        WorkoutType.Endurance => "Light weight, high reps. Build muscular endurance.",
        WorkoutType.MetabolicConditioning => "Circuit-style, short rest. Burn calories and improve conditioning.",
        WorkoutType.Recovery => "Light work to promote blood flow and recovery.",
        WorkoutType.Mobility => "Flexibility and joint health focus.",
        WorkoutType.HIIT => "High-intensity intervals. Maximum effort, short rest.",
        WorkoutType.Deload => "Reduced volume and intensity. Let your body recover.",
        _ => "General training session."
    };
}
