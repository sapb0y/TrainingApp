using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class InterferenceCalculatorService
{
    public record CardioStress(decimal Trimp, CardioIntensityZone Zone, string ZoneLabel);
    public record InterferenceScore(decimal Score, string Level, string Explanation);
    public record SequencingResult(string RecommendedOrder, int SeparationHours, string Rationale);
    public record DailyTrainingSummary(int StrengthSessions, int CardioSessions, decimal StrengthTrimp, decimal CardioTrimp, decimal TotalTrimp, decimal InterferenceScore, string InterferenceLevel);
    public record WeeklyCardioStats(int TotalSessions, int TotalMinutes, decimal TotalDistanceKm, decimal TotalTrimp, Dictionary<string, int> MinutesByZone);

    private static readonly Dictionary<CardioModality, decimal> ModalityFactors = new()
    {
        [CardioModality.Running] = 2.5m,
        [CardioModality.Cycling] = 1.0m,
        [CardioModality.Rowing] = 1.5m,
        [CardioModality.Swimming] = 1.2m,
        [CardioModality.Walking] = 0.5m,
        [CardioModality.Elliptical] = 0.8m,
        [CardioModality.Other] = 1.0m
    };

    private static readonly Dictionary<CardioIntensityZone, decimal> ZoneFactors = new()
    {
        [CardioIntensityZone.Zone1] = 0.5m,
        [CardioIntensityZone.Zone2] = 0.8m,
        [CardioIntensityZone.Zone3] = 1.0m,
        [CardioIntensityZone.Zone4] = 1.5m,
        [CardioIntensityZone.Zone5] = 2.0m
    };

    private static readonly HashSet<string> LowerBodyMuscles = new(StringComparer.OrdinalIgnoreCase)
    {
        "quadriceps", "quads", "hamstrings", "glutes", "calves", "hip flexors", "adductors", "abductors", "legs"
    };

    public static CardioStress CalculateCardioTrimp(CardioModality modality, CardioIntensityZone zone, int durationMinutes)
    {
        var modalityFactor = ModalityFactors.GetValueOrDefault(modality, 1.0m);
        var zoneFactor = ZoneFactors.GetValueOrDefault(zone, 1.0m);
        var trimp = durationMinutes * zoneFactor * modalityFactor / 60m;

        var zoneLabel = zone switch
        {
            CardioIntensityZone.Zone1 => "Recovery",
            CardioIntensityZone.Zone2 => "Aerobic",
            CardioIntensityZone.Zone3 => "Tempo",
            CardioIntensityZone.Zone4 => "Threshold",
            CardioIntensityZone.Zone5 => "VO2max",
            _ => "Unknown"
        };

        return new CardioStress(Math.Round(trimp, 2), zone, zoneLabel);
    }

    public static InterferenceScore CalculateInterferenceScore(
        CardioModality modality, CardioIntensityZone zone, int durationMinutes, List<string> muscleGroupsWorked)
    {
        var baseFactor = ModalityFactors.GetValueOrDefault(modality, 1.0m);

        var zoneAmplifier = zone switch
        {
            CardioIntensityZone.Zone1 or CardioIntensityZone.Zone2 => 0.5m,
            CardioIntensityZone.Zone3 => 1.0m,
            CardioIntensityZone.Zone4 => 1.3m,
            CardioIntensityZone.Zone5 => 1.5m,
            _ => 1.0m
        };

        var durationAmplifier = durationMinutes switch
        {
            <= 30 => 0.8m,
            <= 45 => 1.0m,
            <= 60 => 1.2m,
            _ => 1.5m
        };

        var hasLowerOverlap = muscleGroupsWorked.Any(m => LowerBodyMuscles.Contains(m));
        var overlapFactor = (modality, hasLowerOverlap) switch
        {
            (CardioModality.Running, true) => 1.5m,
            (CardioModality.Cycling, true) => 1.2m,
            (_, false) => 0.5m,
            _ => 1.0m
        };

        var score = Math.Clamp(baseFactor * zoneAmplifier * durationAmplifier * overlapFactor, 0m, 10m);

        var level = score switch
        {
            < 3m => "Low",
            < 6m => "Moderate",
            < 8m => "High",
            _ => "Very High"
        };

        var explanation = $"{modality} {zone} for {durationMinutes}min — {level} interference" +
            (hasLowerOverlap ? " (lower body overlap)" : "");

        return new InterferenceScore(Math.Round(score, 2), level, explanation);
    }

    public static SequencingResult RecommendSequencing(
        bool hasStrength, bool hasCardio, CardioModality? modality, CardioIntensityZone? zone)
    {
        if (!hasStrength && hasCardio)
            return new SequencingResult("Any", 0, "No strength session — cardio order flexible.");

        if (hasStrength && !hasCardio)
            return new SequencingResult("Any", 0, "No cardio session — proceed with strength training.");

        if (!hasStrength && !hasCardio)
            return new SequencingResult("Any", 0, "Rest day — no sessions planned.");

        var isHighIntensity = zone is CardioIntensityZone.Zone4 or CardioIntensityZone.Zone5;
        var isRunning = modality == CardioModality.Running;

        if (isHighIntensity || (isRunning && zone >= CardioIntensityZone.Zone3))
        {
            return new SequencingResult(
                "Separate days or 6+ hours apart",
                6,
                "High-intensity cardio or running at tempo+ — separate from strength to minimize AMPK/mTOR interference.");
        }

        return new SequencingResult(
            "Strength first, cardio after",
            3,
            "Low-moderate cardio — perform strength first. 3+ hour separation ideal (Baar principle).");
    }

    public static CardioIntensityZone CalculateHeartRateZone(int heartRate, int maxHr)
    {
        if (maxHr <= 0) return CardioIntensityZone.Zone2;
        var pct = (decimal)heartRate / maxHr * 100m;
        return pct switch
        {
            < 60m => CardioIntensityZone.Zone1,
            < 70m => CardioIntensityZone.Zone2,
            < 80m => CardioIntensityZone.Zone3,
            < 90m => CardioIntensityZone.Zone4,
            _ => CardioIntensityZone.Zone5
        };
    }

    public static int EstimateMaxHr(int age)
    {
        // Tanaka formula
        return 208 - (int)(0.7 * age);
    }

    public static DailyTrainingSummary CalculateDailySummary(
        decimal strengthTrimp, int strengthCount, decimal cardioTrimp, int cardioCount, decimal interferenceScore, string interferenceLevel)
    {
        return new DailyTrainingSummary(
            strengthCount, cardioCount, strengthTrimp, cardioTrimp,
            strengthTrimp + cardioTrimp, interferenceScore, interferenceLevel);
    }

    public static WeeklyCardioStats CalculateWeeklySummary(
        IEnumerable<(CardioModality Modality, CardioIntensityZone Zone, int DurationMinutes, decimal? DistanceKm, decimal Trimp)> sessions)
    {
        var totalSessions = 0;
        var totalMinutes = 0;
        var totalDistance = 0m;
        var totalTrimp = 0m;
        var minutesByZone = new Dictionary<string, int>();

        foreach (var (modality, zone, duration, distance, trimp) in sessions)
        {
            totalSessions++;
            totalMinutes += duration;
            totalDistance += distance ?? 0m;
            totalTrimp += trimp;

            var zoneName = zone.ToString();
            minutesByZone[zoneName] = minutesByZone.GetValueOrDefault(zoneName, 0) + duration;
        }

        return new WeeklyCardioStats(totalSessions, totalMinutes, totalDistance, Math.Round(totalTrimp, 2), minutesByZone);
    }
}
