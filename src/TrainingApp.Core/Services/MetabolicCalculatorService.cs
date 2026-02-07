using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class MetabolicCalculatorService
{
    public record BmrResult(decimal BmrKcal, string Formula);
    public record TdeeResult(decimal TdeeKcal, decimal ActivityMultiplier, decimal BmrKcal);
    public record AdaptationResult(decimal AdaptedTdeeKcal, decimal AdaptationKcal, decimal AdaptationPercent);
    public record NeatCompensation(decimal BaselineNeatKcal, decimal CurrentNeatKcal, decimal CompensationKcal, decimal CompensationPercent);
    public record IntakeTarget(decimal TargetKcal, decimal DeficitKcal, decimal DeficitPercent);
    public record WeightProjection(DateOnly Date, decimal ProjectedWeightKg);

    public static BmrResult CalculateBmr(BiologicalSex sex, decimal weightKg, decimal heightCm, int ageYears)
    {
        // Mifflin-St Jeor
        var bmr = sex switch
        {
            BiologicalSex.Male => 10m * weightKg + 6.25m * heightCm - 5m * ageYears + 5m,
            BiologicalSex.Female => 10m * weightKg + 6.25m * heightCm - 5m * ageYears - 161m,
            _ => 10m * weightKg + 6.25m * heightCm - 5m * ageYears + 5m
        };

        bmr = Math.Clamp(bmr, 800m, 5000m);
        return new BmrResult(Math.Round(bmr, 1), "Mifflin-St Jeor");
    }

    public static TdeeResult CalculateTdee(decimal bmrKcal, ActivityLevel level)
    {
        var multiplier = level switch
        {
            ActivityLevel.Sedentary => 1.2m,
            ActivityLevel.Light => 1.375m,
            ActivityLevel.Moderate => 1.55m,
            ActivityLevel.Active => 1.725m,
            ActivityLevel.VeryActive => 1.9m,
            _ => 1.55m
        };

        var tdee = Math.Round(bmrKcal * multiplier, 1);
        return new TdeeResult(tdee, multiplier, bmrKcal);
    }

    public static AdaptationResult EstimateAdaptation(decimal originalTdee, decimal currentWeightKg, decimal startWeightKg, int weeksInDeficit)
    {
        var weightLost = startWeightKg - currentWeightKg;
        if (weightLost <= 0)
            return new AdaptationResult(originalTdee, 0m, 0m);

        // Hall 2008: ~15 kcal/kg lost + ~50 kcal per 4 weeks hormonal
        var weightBasedAdaptation = weightLost * 15m;
        var timeBasedAdaptation = weeksInDeficit / 4m * 50m;
        var totalAdaptation = weightBasedAdaptation + timeBasedAdaptation;

        // Clamp to max 20% of original TDEE
        var maxAdaptation = originalTdee * 0.20m;
        totalAdaptation = Math.Min(totalAdaptation, maxAdaptation);

        var adaptationPercent = originalTdee > 0 ? totalAdaptation / originalTdee * 100m : 0m;
        var adaptedTdee = originalTdee - totalAdaptation;

        return new AdaptationResult(
            Math.Round(adaptedTdee, 1),
            Math.Round(totalAdaptation, 1),
            Math.Round(adaptationPercent, 1));
    }

    public static NeatCompensation EstimateNeatCompensation(int currentSteps, int baselineSteps = 8000)
    {
        var baselineNeat = baselineSteps * 0.04m;
        var currentNeat = currentSteps * 0.04m;

        decimal compensationKcal;
        if (currentSteps < baselineSteps)
            compensationKcal = (baselineSteps - currentSteps) * 0.04m;
        else
            compensationKcal = 0m;

        var compensationPercent = baselineNeat > 0 ? compensationKcal / baselineNeat * 100m : 0m;

        return new NeatCompensation(
            Math.Round(baselineNeat, 1),
            Math.Round(currentNeat, 1),
            Math.Round(compensationKcal, 1),
            Math.Round(compensationPercent, 1));
    }

    public static IntakeTarget CalculateIntakeTarget(decimal adjustedTdee, decimal weeklyRateKg, decimal bmrFloor)
    {
        // 1 kg fat ≈ 7700 kcal → daily deficit = weeklyRateKg × 1100
        var dailyDeficit = weeklyRateKg * 1100m;
        var target = adjustedTdee - dailyDeficit;

        // Never recommend below BMR
        target = Math.Max(target, bmrFloor);
        var actualDeficit = adjustedTdee - target;
        var deficitPercent = adjustedTdee > 0 ? actualDeficit / adjustedTdee * 100m : 0m;

        return new IntakeTarget(
            Math.Round(target, 1),
            Math.Round(actualDeficit, 1),
            Math.Round(deficitPercent, 1));
    }

    public static decimal CalculateWeeklyRate(decimal avg7dOlder, decimal avg7dNewer)
    {
        return Math.Round(avg7dNewer - avg7dOlder, 2);
    }

    public static List<WeightProjection> ProjectWeight(decimal currentWeightKg, decimal weeklyRateKg, int weeks)
    {
        var projections = new List<WeightProjection>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var i = 1; i <= weeks; i++)
        {
            var date = today.AddDays(i * 7);
            var projected = Math.Round(currentWeightKg + weeklyRateKg * i, 1);
            projections.Add(new WeightProjection(date, projected));
        }

        return projections;
    }

    public static decimal CalculateEma(decimal previousEma, decimal newValue, int period)
    {
        var alpha = 2m / (period + 1m);
        return Math.Round(alpha * newValue + (1m - alpha) * previousEma, 2);
    }
}
