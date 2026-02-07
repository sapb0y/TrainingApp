using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class TrainingStressService
{
    public record SetStress(decimal Weight, int Reps, decimal Rpe, decimal Trimp);
    public record SessionStress(decimal Trimp, int TotalSets, int TotalReps, decimal TotalVolume, decimal AverageRpe);
    public record DailyStress(decimal Trimp, int TotalSets, int TotalReps, decimal TotalVolume, int WorkoutCount, decimal? AverageSessionRpe);
    public record FatigueState(decimal Ctl, decimal Atl, decimal Tsb);
    public record ReadinessResult(decimal Score, string Category, string Recommendation);

    public static decimal EstimateRpe(decimal? rpe, int? rir)
    {
        if (rpe.HasValue)
            return Math.Clamp(rpe.Value, 1m, 10m);

        if (rir.HasValue)
            return Math.Clamp(10m - rir.Value, 1m, 10m);

        return 8m;
    }

    public static SetStress CalculateSetStress(decimal? actualWeight, int? actualReps, decimal? rpe, int? rir, bool isWarmup)
    {
        if (isWarmup || actualWeight is null || actualReps is null || actualWeight <= 0 || actualReps <= 0)
            return new SetStress(0, 0, 0, 0);

        var effectiveRpe = EstimateRpe(rpe, rir);
        var trimp = actualWeight.Value * actualReps.Value * (effectiveRpe / 10m) / 100m;

        return new SetStress(actualWeight.Value, actualReps.Value, effectiveRpe, trimp);
    }

    public static SessionStress CalculateSessionStress(IEnumerable<(decimal? ActualWeight, int? ActualReps, decimal? Rpe, int? Rir, bool IsWarmup)> sets)
    {
        var totalTrimp = 0m;
        var totalSets = 0;
        var totalReps = 0;
        var totalVolume = 0m;
        var rpeSum = 0m;
        var rpeCount = 0;

        foreach (var (weight, reps, rpe, rir, isWarmup) in sets)
        {
            var setStress = CalculateSetStress(weight, reps, rpe, rir, isWarmup);
            if (setStress.Trimp <= 0) continue;

            totalTrimp += setStress.Trimp;
            totalSets++;
            totalReps += setStress.Reps;
            totalVolume += setStress.Weight * setStress.Reps;
            rpeSum += setStress.Rpe;
            rpeCount++;
        }

        var avgRpe = rpeCount > 0 ? rpeSum / rpeCount : 0m;
        return new SessionStress(totalTrimp, totalSets, totalReps, totalVolume, avgRpe);
    }

    public static DailyStress CalculateDailyStress(IEnumerable<SessionStress> sessions, IEnumerable<int?> sessionRpes)
    {
        var totalTrimp = 0m;
        var totalSets = 0;
        var totalReps = 0;
        var totalVolume = 0m;
        var count = 0;

        foreach (var s in sessions)
        {
            totalTrimp += s.Trimp;
            totalSets += s.TotalSets;
            totalReps += s.TotalReps;
            totalVolume += s.TotalVolume;
            count++;
        }

        var validRpes = sessionRpes.Where(r => r.HasValue).Select(r => r!.Value).ToList();
        var avgSessionRpe = validRpes.Count > 0 ? (decimal?)validRpes.Average() : null;

        return new DailyStress(totalTrimp, totalSets, totalReps, totalVolume, count, avgSessionRpe);
    }

    public static FatigueState UpdateFatigueState(decimal prevCtl, decimal prevAtl, decimal trimp, decimal ctlDecay = 42m, decimal atlDecay = 7m)
    {
        var ctlAlpha = 1m - (decimal)Math.Exp(-1.0 / (double)ctlDecay);
        var atlAlpha = 1m - (decimal)Math.Exp(-1.0 / (double)atlDecay);

        var ctl = prevCtl * (1m - ctlAlpha) + trimp * ctlAlpha;
        var atl = prevAtl * (1m - atlAlpha) + trimp * atlAlpha;
        var tsb = ctl - atl;

        return new FatigueState(ctl, atl, tsb);
    }

    public static ReadinessResult CalculateReadiness(
        decimal tsb,
        RecoveryCapacity capacity,
        int? sleepQuality = null,
        int? stressLevel = null,
        int? energyLevel = null,
        int? muscleReadiness = null,
        int? mood = null)
    {
        // Sigmoid mapping: TSB → base score 1-10
        // TSB of 0 → ~5.5, TSB of +20 → ~8, TSB of -20 → ~3
        var sigmoid = 1.0 / (1.0 + Math.Exp(-0.1 * (double)tsb));
        var baseScore = 1m + 9m * (decimal)sigmoid;

        // Recovery capacity shift
        var capacityShift = capacity switch
        {
            RecoveryCapacity.High => 0.5m,
            RecoveryCapacity.Low => -0.5m,
            _ => 0m
        };

        // Recovery log modifiers (each field 1-5, neutral at 3)
        var recoveryAdj = 0m;
        var recoveryCount = 0;

        if (sleepQuality.HasValue) { recoveryAdj += (sleepQuality.Value - 3) * 0.3m; recoveryCount++; }
        if (stressLevel.HasValue) { recoveryAdj += (3 - stressLevel.Value) * 0.25m; recoveryCount++; } // inverted: high stress = bad
        if (energyLevel.HasValue) { recoveryAdj += (energyLevel.Value - 3) * 0.2m; recoveryCount++; }
        if (muscleReadiness.HasValue) { recoveryAdj += (muscleReadiness.Value - 3) * 0.2m; recoveryCount++; }
        if (mood.HasValue) { recoveryAdj += (mood.Value - 3) * 0.1m; recoveryCount++; }

        var score = Math.Clamp(baseScore + capacityShift + recoveryAdj, 1m, 10m);

        var (category, recommendation) = score switch
        {
            >= 8m => ("Excellent", "Full intensity training recommended."),
            >= 6m => ("Good", "Normal training load appropriate."),
            >= 4m => ("Moderate", "Consider reducing volume or intensity."),
            >= 3m => ("Poor", "Light training or active recovery recommended."),
            _ => ("Very Poor", "Rest day strongly recommended.")
        };

        return new ReadinessResult(Math.Round(score, 1), category, recommendation);
    }
}
