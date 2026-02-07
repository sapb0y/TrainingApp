namespace TrainingApp.Core.Services;

public static class FatigueRuleEngine
{
    public record FatigueAlert(string RuleName, string Scope, AlertSeverity Severity, string Message, string InputSummary, string OutputSummary);
    public enum AlertSeverity { Info, Warning, Alert }

    public static List<FatigueAlert> EvaluateRules(decimal tsb, decimal? readinessScore, int daysSinceDeload, int daysSinceLastWorkout)
    {
        var alerts = new List<FatigueAlert>();

        // TSB Overreaching Warning (check first — more severe)
        if (tsb < -30)
        {
            alerts.Add(new FatigueAlert(
                "TSB Overreaching Warning",
                "InterWorkout",
                AlertSeverity.Alert,
                "High fatigue alert. Take rest days to prevent overtraining.",
                $"TSB={tsb:F1}",
                "Recommend rest days"));
        }
        // TSB Deload Suggestion
        else if (tsb < -20)
        {
            alerts.Add(new FatigueAlert(
                "TSB Deload Suggestion",
                "InterWorkout",
                AlertSeverity.Warning,
                "TSB indicates high fatigue. Consider a deload week.",
                $"TSB={tsb:F1}",
                "Recommend deload week"));
        }

        // Deload Overdue
        if (daysSinceDeload > 28)
        {
            alerts.Add(new FatigueAlert(
                "Deload Overdue",
                "InterWorkout",
                AlertSeverity.Warning,
                $"No deload in {daysSinceDeload} days. Schedule a lighter week.",
                $"DaysSinceDeload={daysSinceDeload}",
                "Recommend scheduling deload"));
        }

        // Poor Recovery
        if (readinessScore.HasValue && readinessScore.Value < 3)
        {
            alerts.Add(new FatigueAlert(
                "Poor Recovery",
                "PreWorkout",
                AlertSeverity.Warning,
                "Low readiness. Consider lighter workout or rest.",
                $"ReadinessScore={readinessScore.Value:F1}",
                "Recommend lighter workout"));
        }

        // Detraining Risk
        if (tsb > 15 && daysSinceLastWorkout > 7)
        {
            alerts.Add(new FatigueAlert(
                "Detraining Risk",
                "InterWorkout",
                AlertSeverity.Info,
                "Extended recovery — consider resuming training.",
                $"TSB={tsb:F1}, DaysSinceWorkout={daysSinceLastWorkout}",
                "Recommend resuming training"));
        }

        return alerts;
    }
}
