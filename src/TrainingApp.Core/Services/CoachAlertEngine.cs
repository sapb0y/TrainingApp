namespace TrainingApp.Core.Services;

public static class CoachAlertEngine
{
    public record CoachAlert(Guid AthleteId, string AthleteName,
        CoachAlertCategory Category, string Severity, string Message,
        Dictionary<string, object>? Data);

    public enum CoachAlertCategory { FatigueRisk, OverreachingRisk, MissedSessions,
        RPEDrift, DeficitStress, GoalStalled }

    public static CoachAlert? EvaluateFatigue(Guid athleteId, string athleteName, decimal? tsb, decimal? readinessScore)
    {
        if (tsb.HasValue && tsb.Value < -10)
            return new CoachAlert(athleteId, athleteName, CoachAlertCategory.FatigueRisk, "Warning",
                $"TSB is {tsb.Value:F1} — high fatigue risk.",
                new Dictionary<string, object> { ["tsb"] = tsb.Value });

        if (readinessScore.HasValue && readinessScore.Value < 50)
            return new CoachAlert(athleteId, athleteName, CoachAlertCategory.FatigueRisk, "Warning",
                $"Readiness score is {readinessScore.Value:F0}% — below threshold.",
                new Dictionary<string, object> { ["readiness"] = readinessScore.Value });

        return null;
    }

    public static CoachAlert? EvaluateOverreaching(Guid athleteId, string athleteName, decimal avgSessionRpe)
    {
        if (avgSessionRpe > 9)
            return new CoachAlert(athleteId, athleteName, CoachAlertCategory.OverreachingRisk, "Warning",
                $"Average session RPE is {avgSessionRpe:F1} (last 3) — overreaching risk.",
                new Dictionary<string, object> { ["avgRpe"] = avgSessionRpe });

        return null;
    }

    public static CoachAlert? EvaluateMissedSessions(Guid athleteId, string athleteName, int missedCount)
    {
        if (missedCount > 2)
            return new CoachAlert(athleteId, athleteName, CoachAlertCategory.MissedSessions, "Info",
                $"{missedCount} planned sessions skipped in last 7 days.",
                new Dictionary<string, object> { ["missed"] = missedCount });

        return null;
    }

    public static CoachAlert? EvaluateRpeDrift(Guid athleteId, string athleteName, decimal avgRirDeviation)
    {
        if (Math.Abs(avgRirDeviation) > 1.5m)
            return new CoachAlert(athleteId, athleteName, CoachAlertCategory.RPEDrift, "Warning",
                $"Average RIR drift is {avgRirDeviation:F1} — targets may need adjustment.",
                new Dictionary<string, object> { ["rirDrift"] = avgRirDeviation });

        return null;
    }

    public static CoachAlert? EvaluateDeficitStress(Guid athleteId, string athleteName, bool inDeficit, decimal strengthChangePct)
    {
        if (inDeficit && strengthChangePct < -5)
            return new CoachAlert(athleteId, athleteName, CoachAlertCategory.DeficitStress, "Alert",
                $"In deficit phase with {strengthChangePct:F1}% strength decline.",
                new Dictionary<string, object> { ["strengthChange"] = strengthChangePct });

        return null;
    }

    public static CoachAlert? EvaluateGoalStalled(Guid athleteId, string athleteName, string goalTitle, decimal percentComplete, DateOnly? targetDate)
    {
        if (targetDate.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var totalDays = targetDate.Value.DayNumber - today.AddDays(-(int)(targetDate.Value.DayNumber - today.DayNumber)).DayNumber;
            if (totalDays <= 0) return null;

            var daysElapsed = totalDays - (targetDate.Value.DayNumber - today.DayNumber);
            if (daysElapsed <= 0) return null;

            var expectedProgress = (decimal)daysElapsed / totalDays * 100m;
            if (percentComplete < expectedProgress - 15)
                return new CoachAlert(athleteId, athleteName, CoachAlertCategory.GoalStalled, "Info",
                    $"Goal \"{goalTitle}\" is behind schedule ({percentComplete:F0}% vs {expectedProgress:F0}% expected).",
                    new Dictionary<string, object> { ["goal"] = goalTitle, ["actual"] = percentComplete, ["expected"] = expectedProgress });
        }

        return null;
    }

    private static readonly Dictionary<string, int> SeverityOrder = new()
    {
        ["Alert"] = 0,
        ["Warning"] = 1,
        ["Info"] = 2
    };

    public static List<CoachAlert> SortBySeverity(List<CoachAlert> alerts)
    {
        return alerts.OrderBy(a => SeverityOrder.GetValueOrDefault(a.Severity, 99))
            .ThenBy(a => a.AthleteName)
            .ToList();
    }
}
