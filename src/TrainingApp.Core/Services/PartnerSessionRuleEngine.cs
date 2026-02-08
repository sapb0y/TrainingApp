using static TrainingApp.Core.Services.FatigueRuleEngine;
using static TrainingApp.Core.Services.PartnerSchedulingService;

namespace TrainingApp.Core.Services;

public static class PartnerSessionRuleEngine
{
    public static List<FatigueAlert> EvaluateRules(
        decimal? tsbA, decimal? tsbB,
        int exerciseCountA, int exerciseCountB,
        PartnerSchedule schedule)
    {
        var alerts = new List<FatigueAlert>();

        // Fatigue Mismatch
        if (tsbA.HasValue && tsbB.HasValue && Math.Abs(tsbA.Value - tsbB.Value) > 20)
        {
            alerts.Add(new FatigueAlert(
                "Fatigue Mismatch",
                "PartnerSession",
                AlertSeverity.Warning,
                "Large fatigue difference between partners. One may struggle to keep up.",
                $"TSB_A={tsbA.Value:F1}, TSB_B={tsbB.Value:F1}, Diff={Math.Abs(tsbA.Value - tsbB.Value):F1}",
                "Consider adjusting individual intensities"));
        }

        // Volume Imbalance
        if (exerciseCountA > 0 && exerciseCountB > 0
            && (exerciseCountA >= 2 * exerciseCountB || exerciseCountB >= 2 * exerciseCountA))
        {
            var higher = Math.Max(exerciseCountA, exerciseCountB);
            var lower = Math.Min(exerciseCountA, exerciseCountB);
            alerts.Add(new FatigueAlert(
                "Volume Imbalance",
                "PartnerSession",
                AlertSeverity.Info,
                "One partner has significantly more exercises. Session may feel unbalanced.",
                $"ExercisesA={exerciseCountA}, ExercisesB={exerciseCountB}",
                "Balance workout volumes or plan extra rest for the lighter partner"));
        }

        // High Equipment Conflicts
        var totalBlocks = schedule.Blocks.Count;
        if (totalBlocks > 0 && schedule.Summary.EquipmentConflicts > 0)
        {
            var conflictPct = (decimal)schedule.Summary.EquipmentConflicts / totalBlocks * 100;
            if (conflictPct > 30)
            {
                alerts.Add(new FatigueAlert(
                    "High Equipment Conflicts",
                    "PartnerSession",
                    AlertSeverity.Info,
                    "Many equipment conflicts reduce parallel efficiency.",
                    $"Conflicts={schedule.Summary.EquipmentConflicts}, Blocks={totalBlocks}, Pct={conflictPct:F0}%",
                    "Use different equipment for each partner's exercises"));
            }
        }

        // Low Partner Utilization
        if (totalBlocks > 0)
        {
            if (schedule.Summary.UtilizationPercentA < 50 || schedule.Summary.UtilizationPercentB < 50)
            {
                alerts.Add(new FatigueAlert(
                    "Low Partner Utilization",
                    "PartnerSession",
                    AlertSeverity.Info,
                    "One partner spends too much time idle. Consider rebalancing workouts.",
                    $"UtilA={schedule.Summary.UtilizationPercentA:F1}%, UtilB={schedule.Summary.UtilizationPercentB:F1}%",
                    "Add exercises for the under-utilized partner"));
            }
        }

        return alerts;
    }
}
