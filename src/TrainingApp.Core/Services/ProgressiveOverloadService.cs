using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class ProgressiveOverloadService
{
    public record ProgressionRecommendation(
        decimal Weight,
        int Reps,
        ProgressionType Type,
        string Rationale);

    public enum ProgressionType
    {
        WeightIncrease,
        RepIncrease,
        Maintain,
        Deload
    }

    public static ProgressionRecommendation CalculateProgression(
        decimal prevWeight,
        int prevReps,
        int targetRir,
        int actualRir,
        ProgramGoal goal)
    {
        var (repMin, repMax) = LoadPrescriptionService.GetRepRange(goal);
        var increment = GetMinWeightIncrement("default");

        // Performing much better than target (actualRir > targetRir + 1) → increase weight
        if (actualRir > targetRir + 1)
        {
            var newWeight = LoadPrescriptionService.RoundToIncrement(prevWeight + increment);
            return new(newWeight, repMin, ProgressionType.WeightIncrease,
                $"RIR {actualRir} well above target {targetRir} — increase weight to {newWeight}kg, reset reps to {repMin}");
        }

        // At or near target RIR — try to add reps if not maxed
        if (actualRir >= targetRir)
        {
            if (prevReps < repMax)
            {
                return new(prevWeight, prevReps + 1, ProgressionType.RepIncrease,
                    $"On target RIR — increase reps from {prevReps} to {prevReps + 1}");
            }

            // Reps maxed out → increase weight and reset reps
            var newWeight = LoadPrescriptionService.RoundToIncrement(prevWeight + increment);
            return new(newWeight, repMin, ProgressionType.WeightIncrease,
                $"Reps maxed at {repMax} — increase weight to {newWeight}kg, reset reps to {repMin}");
        }

        // Struggling (actualRir < targetRir) → maintain
        return new(prevWeight, prevReps, ProgressionType.Maintain,
            $"RIR {actualRir} below target {targetRir} — maintain current weight and reps");
    }

    public static decimal GetMinWeightIncrement(string muscleGroup)
    {
        return muscleGroup switch
        {
            "Biceps" or "Triceps" or "Forearms" or "Calves" or "Abs" => 1.25m,
            _ => 2.5m,
        };
    }
}
