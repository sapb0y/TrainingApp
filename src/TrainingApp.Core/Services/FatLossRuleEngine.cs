using static TrainingApp.Core.Services.FatigueRuleEngine;

namespace TrainingApp.Core.Services;

public static class FatLossRuleEngine
{
    public static List<FatigueAlert> EvaluateRules(
        decimal? weeklyRateKg,
        int weeksInDeficit,
        bool isDeficitActive,
        decimal adaptationPercent,
        decimal neatCompensationPercent,
        int? daysSinceBreak,
        int? breakIntervalDays)
    {
        var alerts = new List<FatigueAlert>();

        // Rapid Weight Loss
        if (weeklyRateKg.HasValue && weeklyRateKg.Value < -1.0m)
        {
            alerts.Add(new FatigueAlert(
                "Rapid Weight Loss",
                "FatLoss",
                AlertSeverity.Warning,
                "Losing weight too fast. Risk of muscle loss.",
                $"WeeklyRate={weeklyRateKg.Value:F2}kg/wk",
                "Reduce deficit or add diet break"));
        }

        // Stalled Weight Loss (only when deficit active > 2 weeks)
        if (isDeficitActive && weeksInDeficit > 2 && weeklyRateKg.HasValue && weeklyRateKg.Value > -0.1m)
        {
            alerts.Add(new FatigueAlert(
                "Stalled Weight Loss",
                "FatLoss",
                AlertSeverity.Info,
                "Weight loss has stalled. Consider diet break or reassess.",
                $"WeeklyRate={weeklyRateKg.Value:F2}kg/wk, Weeks={weeksInDeficit}",
                "Reassess intake or add refeed"));
        }

        // Metabolic Adaptation High
        if (isDeficitActive && adaptationPercent > 15m)
        {
            alerts.Add(new FatigueAlert(
                "Metabolic Adaptation High",
                "FatLoss",
                AlertSeverity.Warning,
                "Significant metabolic adaptation detected. Diet break recommended.",
                $"Adaptation={adaptationPercent:F1}%",
                "Schedule diet break at maintenance"));
        }

        // NEAT Decline
        if (neatCompensationPercent > 20m)
        {
            alerts.Add(new FatigueAlert(
                "NEAT Decline",
                "FatLoss",
                AlertSeverity.Warning,
                "Activity compensation detected. Step count declining.",
                $"NeatCompensation={neatCompensationPercent:F1}%",
                "Increase daily step target"));
        }

        // Diet Break Due
        if (isDeficitActive && breakIntervalDays.HasValue && daysSinceBreak.HasValue && daysSinceBreak.Value > breakIntervalDays.Value)
        {
            alerts.Add(new FatigueAlert(
                "Diet Break Due",
                "FatLoss",
                AlertSeverity.Info,
                "Scheduled diet break due. Consider 1-2 week maintenance.",
                $"DaysSinceBreak={daysSinceBreak.Value}, Interval={breakIntervalDays.Value}d",
                "Take 1-2 week maintenance break"));
        }

        return alerts;
    }
}
