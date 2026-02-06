using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class MesocycleCalculatorService
{
    private const int MinAccumulationWeeks = 3;
    private const int MaxAccumulationWeeks = 10;
    private const int DeloadWeeks = 1;

    public static int CalculateAccumulationWeeks(TrainingExperience experience, int? age, RecoveryCapacity recovery)
    {
        int baseWeeks = experience switch
        {
            TrainingExperience.Beginner => 8,
            TrainingExperience.Intermediate => 5,
            TrainingExperience.Advanced => 4,
            _ => 5,
        };

        int ageMod = age switch
        {
            null => 0,
            < 30 => 0,
            < 40 => 0,
            < 50 => -1,
            < 60 => -1,
            _ => -2,
        };

        int recoveryMod = recovery switch
        {
            RecoveryCapacity.High => 1,
            RecoveryCapacity.Normal => 0,
            RecoveryCapacity.Low => -1,
            _ => 0,
        };

        int result = baseWeeks + ageMod + recoveryMod;
        return Math.Clamp(result, MinAccumulationWeeks, MaxAccumulationWeeks);
    }

    public static List<(PhaseType Type, int Weeks)> GeneratePhaseStructure(int totalWeeks, int accumulationWeeks)
    {
        if (totalWeeks <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalWeeks), "Total weeks must be > 0");
        if (accumulationWeeks <= 0)
            throw new ArgumentOutOfRangeException(nameof(accumulationWeeks), "Accumulation weeks must be > 0");

        var phases = new List<(PhaseType Type, int Weeks)>();
        int remaining = totalWeeks;

        while (remaining > 0)
        {
            int accumWeeks = Math.Min(accumulationWeeks, remaining);
            phases.Add((PhaseType.Accumulation, accumWeeks));
            remaining -= accumWeeks;

            if (remaining >= DeloadWeeks)
            {
                phases.Add((PhaseType.Deload, DeloadWeeks));
                remaining -= DeloadWeeks;
            }
        }

        return phases;
    }
}
