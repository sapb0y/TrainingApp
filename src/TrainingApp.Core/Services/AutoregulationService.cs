namespace TrainingApp.Core.Services;

public static class AutoregulationService
{
    public record SetEvaluation(LoadAdjustment? Load, string? Reason);
    public record LoadAdjustment(decimal RecommendedWeight, decimal AdjustmentPercent, AdjustmentType Type);
    public enum AdjustmentType { Reduce, Increase, Maintain }

    public record WorkoutEvaluation(List<ExerciseRecommendation> Exercises, string? SessionNote);
    public record ExerciseRecommendation(Guid ExerciseId, VolumeAction Volume, SetPrescription? NextSet, string Reason);
    public enum VolumeAction { Continue, AddSet, SkipRemaining }
    public record SetPrescription(decimal Weight, int Reps, int TargetRir);

    public static SetEvaluation EvaluateSet(int? targetRir, int? actualRir, decimal currentWeight, decimal weightIncrement)
    {
        if (targetRir is null || actualRir is null)
            return new SetEvaluation(null, null);

        var drift = actualRir.Value - targetRir.Value;

        if (drift <= -2)
        {
            var adjustPct = -5m;
            var recommended = Math.Round(currentWeight * (1 + adjustPct / 100m) / weightIncrement) * weightIncrement;
            return new SetEvaluation(
                new LoadAdjustment(recommended, adjustPct, AdjustmentType.Reduce),
                $"RIR drift {drift} (too hard) — reduce weight {adjustPct}%");
        }

        if (drift >= 2)
        {
            var adjustPct = 5m;
            var recommended = Math.Round(currentWeight * (1 + adjustPct / 100m) / weightIncrement) * weightIncrement;
            return new SetEvaluation(
                new LoadAdjustment(recommended, adjustPct, AdjustmentType.Increase),
                $"RIR drift +{drift} (too easy) — increase weight +{adjustPct}%");
        }

        return new SetEvaluation(
            new LoadAdjustment(currentWeight, 0m, AdjustmentType.Maintain),
            "On target — maintain weight");
    }

    public static VolumeAction EvaluateExerciseVolume(List<(int? TargetRir, int? ActualRir)> completedSets, int targetSets)
    {
        var validDrifts = completedSets
            .Where(s => s.TargetRir.HasValue && s.ActualRir.HasValue)
            .Select(s => s.ActualRir!.Value - s.TargetRir!.Value)
            .ToList();

        if (validDrifts.Count == 0)
            return VolumeAction.Continue;

        var avgDrift = validDrifts.Average();

        if (avgDrift <= -2 && completedSets.Count >= 3)
            return VolumeAction.SkipRemaining;

        if (avgDrift >= 1 && completedSets.Count >= targetSets)
            return VolumeAction.AddSet;

        return VolumeAction.Continue;
    }

    public static string? EvaluateSession(List<int?> sessionRpes, int currentSessionSets)
    {
        var validRpes = sessionRpes.Where(r => r.HasValue).Select(r => r!.Value).ToList();

        if (validRpes.Count < 3)
            return null;

        var recentThree = validRpes.TakeLast(3).ToList();
        var avgRpe = recentThree.Average();

        if (avgRpe > 9)
            return "Consider scheduling a deload — average session RPE above 9 over last 3 sessions";

        return null;
    }
}
