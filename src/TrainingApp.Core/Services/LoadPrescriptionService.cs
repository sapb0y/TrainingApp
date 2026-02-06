using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class LoadPrescriptionService
{
    public static (int Min, int Max) GetRepRange(ProgramGoal goal) => goal switch
    {
        ProgramGoal.Strength => (3, 6),
        ProgramGoal.Hypertrophy => (8, 12),
        ProgramGoal.PowerBuilding => (5, 8),
        ProgramGoal.GeneralFitness => (8, 15),
        _ => (8, 12),
    };

    /// <summary>
    /// Inverse Epley: estimates %1RM for a given rep count.
    /// Formula: percentage = 1 / (1 + reps/30)
    /// </summary>
    public static decimal EstimatePercentageForReps(int reps)
    {
        if (reps <= 0)
            throw new ArgumentOutOfRangeException(nameof(reps), "Reps must be > 0");

        return Math.Round(1m / (1m + reps / 30m) * 100m, 1);
    }

    /// <summary>
    /// Calculates target weight from e1RM and target reps.
    /// </summary>
    public static decimal CalculateTargetWeight(decimal e1rm, int targetReps)
    {
        if (e1rm <= 0)
            throw new ArgumentOutOfRangeException(nameof(e1rm), "e1RM must be > 0");

        var percentage = EstimatePercentageForReps(targetReps);
        return Math.Round(e1rm * percentage / 100m, 2);
    }

    public static decimal RoundToIncrement(decimal weight, decimal increment = 2.5m)
    {
        if (increment <= 0)
            throw new ArgumentOutOfRangeException(nameof(increment), "Increment must be > 0");

        return Math.Round(weight / increment) * increment;
    }
}
