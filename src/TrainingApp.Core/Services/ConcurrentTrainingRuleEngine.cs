using TrainingApp.Core.Entities;
using static TrainingApp.Core.Services.FatigueRuleEngine;

namespace TrainingApp.Core.Services;

public static class ConcurrentTrainingRuleEngine
{
    private static readonly HashSet<string> LowerBodyMuscles = new(StringComparer.OrdinalIgnoreCase)
    {
        "quadriceps", "quads", "hamstrings", "glutes", "calves", "hip flexors", "adductors", "abductors", "legs"
    };

    public static List<FatigueAlert> EvaluateRules(
        decimal interferenceScore,
        bool hasStrengthToday,
        bool hasCardioToday,
        decimal? separationHours,
        CardioIntensityZone? zone,
        CardioModality? modality,
        List<string> strengthMuscleGroups,
        decimal weeklyCardioTrimp,
        decimal weeklyStrengthTrimp,
        int weeklyZone3PlusPct)
    {
        var alerts = new List<FatigueAlert>();

        // High Interference
        if (interferenceScore > 7m && hasStrengthToday && hasCardioToday)
        {
            alerts.Add(new FatigueAlert(
                "High Interference",
                "ConcurrentTraining",
                AlertSeverity.Warning,
                "High interference between cardio and strength today. Consider separating sessions.",
                $"InterferenceScore={interferenceScore:F1}",
                "Separate sessions or reduce cardio intensity"));
        }

        // Insufficient Separation
        if (separationHours.HasValue && separationHours.Value < 3m
            && zone.HasValue && zone.Value >= CardioIntensityZone.Zone3
            && hasStrengthToday && hasCardioToday)
        {
            alerts.Add(new FatigueAlert(
                "Insufficient Separation",
                "ConcurrentTraining",
                AlertSeverity.Warning,
                "Less than 3 hours between strength and moderate+ cardio. AMPK/mTOR interference likely.",
                $"SeparationHours={separationHours.Value:F1}, Zone={zone.Value}",
                "Increase separation to 3+ hours"));
        }

        // Excessive Cardio Volume
        if (weeklyStrengthTrimp > 0 && weeklyCardioTrimp > 2m * weeklyStrengthTrimp)
        {
            alerts.Add(new FatigueAlert(
                "Excessive Cardio Volume",
                "ConcurrentTraining",
                AlertSeverity.Info,
                "Weekly cardio load exceeds 2x strength load. May impair strength gains.",
                $"CardioTrimp={weeklyCardioTrimp:F1}, StrengthTrimp={weeklyStrengthTrimp:F1}",
                "Reduce cardio volume or increase strength volume"));
        }

        // Running + Leg Day
        if (modality == CardioModality.Running
            && hasStrengthToday && hasCardioToday
            && strengthMuscleGroups.Any(m => LowerBodyMuscles.Contains(m)))
        {
            alerts.Add(new FatigueAlert(
                "Running + Leg Day",
                "ConcurrentTraining",
                AlertSeverity.Info,
                "Running combined with lower body strength. High overlap may impair recovery.",
                $"Modality={modality}, MuscleGroups=[{string.Join(", ", strengthMuscleGroups)}]",
                "Schedule running on upper body or rest days"));
        }

        // Zone Polarization
        if (weeklyZone3PlusPct > 30)
        {
            alerts.Add(new FatigueAlert(
                "Zone Polarization",
                "ConcurrentTraining",
                AlertSeverity.Info,
                "Over 30% of cardio in zone 3+. Polarized training recommends 80/20 low/high split.",
                $"Zone3+Pct={weeklyZone3PlusPct}%",
                "Shift more sessions to zone 1-2"));
        }

        return alerts;
    }
}
