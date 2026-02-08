namespace TrainingApp.Core.Services;

public static class InsightRuleEngine
{
    public record InsightResult(string RuleName, string Category, string Severity, string Title, string Message,
        string? RelatedEntityType = null, Guid? RelatedEntityId = null);

    public static List<InsightResult> EvaluateStrengthInsights(
        IEnumerable<(string ExerciseName, Guid ExerciseId, decimal CurrentBest, decimal PrevBest, decimal AllTimeBest, int SessionCount30d)> exercises)
    {
        var results = new List<InsightResult>();

        foreach (var (name, id, currentBest, prevBest, allTimeBest, sessions) in exercises)
        {
            // New PR
            if (currentBest > allTimeBest && currentBest > 0)
            {
                results.Add(new InsightResult(
                    "New PR", "Strength", "Achievement",
                    $"New PR: {name}",
                    $"New personal record of {currentBest:F1}kg e1RM on {name}, surpassing previous best of {allTimeBest:F1}kg.",
                    "Exercise", id));
            }

            // Strength Plateau
            if (prevBest > 0 && sessions >= 4)
            {
                var changePct = Math.Abs((currentBest - prevBest) / prevBest * 100m);
                if (changePct < 2m)
                {
                    results.Add(new InsightResult(
                        "Strength Plateau", "Strength", "Warning",
                        $"Plateau: {name}",
                        $"{name} e1RM has changed less than 2% over 30 days ({changePct:F1}%). Consider varying rep ranges or intensity.",
                        "Exercise", id));
                }
            }

            // Rapid Progression
            if (prevBest > 0)
            {
                var changePct = (currentBest - prevBest) / prevBest * 100m;
                if (changePct > 10m)
                {
                    results.Add(new InsightResult(
                        "Rapid Progression", "Strength", "Info",
                        $"Rapid gains: {name}",
                        $"{name} e1RM increased {changePct:F1}% in 30 days. Great progress — monitor for injury risk.",
                        "Exercise", id));
                }
            }
        }

        return results;
    }

    public static List<InsightResult> EvaluateVolumeInsights(
        decimal currentWeekVolume, decimal prevWeekVolume,
        IEnumerable<(string MuscleGroup, int SetsPerWeek, int WeeksBelowThreshold)> muscleGroups,
        int sessionsPerWeekLast4Weeks)
    {
        var results = new List<InsightResult>();

        // Volume Spike
        if (prevWeekVolume > 0)
        {
            var changePct = (currentWeekVolume - prevWeekVolume) / prevWeekVolume * 100m;
            if (changePct > 30m)
            {
                results.Add(new InsightResult(
                    "Volume Spike", "Volume", "Warning",
                    "Volume spike detected",
                    $"Training volume increased {changePct:F0}% this week. Rapid volume increases raise injury risk."));
            }
        }

        // Low Muscle Coverage
        foreach (var (group, sets, weeks) in muscleGroups)
        {
            if (sets < 4 && weeks >= 2)
            {
                results.Add(new InsightResult(
                    "Low Muscle Coverage", "Volume", "Info",
                    $"Low volume: {group}",
                    $"{group} has had fewer than 4 sets/week for {weeks}+ weeks. Consider adding volume."));
            }
        }

        // Consistent Training
        if (sessionsPerWeekLast4Weeks >= 4)
        {
            results.Add(new InsightResult(
                "Consistent Training", "Volume", "Achievement",
                "Consistent training streak",
                $"Averaging {sessionsPerWeekLast4Weeks} sessions/week over 4 weeks. Great consistency!"));
        }

        return results;
    }

    public static List<InsightResult> EvaluateRecoveryInsights(
        IEnumerable<(DateOnly Date, decimal Tsb, decimal? ReadinessScore)> dailyData)
    {
        var results = new List<InsightResult>();
        var list = dailyData.OrderBy(d => d.Date).ToList();

        // Overreaching: TSB < -20 for 3+ consecutive days
        var consecutiveOverreach = 0;
        foreach (var (_, tsb, _) in list)
        {
            if (tsb < -20m)
                consecutiveOverreach++;
            else
                consecutiveOverreach = 0;
        }

        if (consecutiveOverreach >= 3)
        {
            results.Add(new InsightResult(
                "Overreaching", "Recovery", "Warning",
                "Overreaching detected",
                $"TSB has been below -20 for {consecutiveOverreach} consecutive days. Consider a deload or rest days."));
        }

        // Fresh & Ready
        var latest = list.LastOrDefault();
        if (latest != default && latest.Tsb > 10m && latest.ReadinessScore > 70m)
        {
            results.Add(new InsightResult(
                "Fresh & Ready", "Recovery", "Info",
                "Fresh and ready to train",
                "TSB is positive and readiness is high. Good time for a hard session or testing."));
        }

        return results;
    }

    public static List<InsightResult> EvaluateWeightInsights(
        decimal? currentWeightKg, decimal? targetWeightKg,
        decimal? weightChange14d, bool isInDeficit, decimal? weeklyRateKg, decimal? targetRateKg)
    {
        var results = new List<InsightResult>();

        // Goal Weight Reached
        if (currentWeightKg.HasValue && targetWeightKg.HasValue && currentWeightKg <= targetWeightKg)
        {
            results.Add(new InsightResult(
                "Goal Weight Reached", "Weight", "Achievement",
                "Goal weight reached!",
                $"Current weight of {currentWeightKg:F1}kg has reached the target of {targetWeightKg:F1}kg. Congratulations!"));
        }

        // Weight Stall
        if (isInDeficit && weightChange14d.HasValue && Math.Abs(weightChange14d.Value) < 0.1m)
        {
            results.Add(new InsightResult(
                "Weight Stall", "Weight", "Warning",
                "Weight loss stall",
                "Weight has changed less than 0.1kg over 14 days during a deficit. Consider adjusting intake or increasing NEAT."));
        }

        // On Track
        if (isInDeficit && weeklyRateKg.HasValue && targetRateKg.HasValue && targetRateKg > 0)
        {
            var rateRatio = Math.Abs(weeklyRateKg.Value) / targetRateKg.Value;
            if (rateRatio >= 0.8m && rateRatio <= 1.2m)
            {
                results.Add(new InsightResult(
                    "On Track", "Weight", "Info",
                    "Weight loss on track",
                    $"Current weekly rate of {weeklyRateKg:F2}kg/week is within 20% of the target rate of {targetRateKg:F2}kg/week."));
            }
        }

        return results;
    }
}
