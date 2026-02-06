using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class VolumeCalculatorService
{
    private static readonly Dictionary<string, (int Mev, int Mav, int Mrv)> VolumeLandmarks = new()
    {
        ["Chest"] = (8, 14, 20),
        ["Back"] = (8, 14, 20),
        ["Shoulders"] = (6, 12, 18),
        ["Quads"] = (6, 12, 18),
        ["Hamstrings"] = (4, 10, 16),
        ["Glutes"] = (4, 10, 16),
        ["Biceps"] = (4, 10, 16),
        ["Triceps"] = (4, 10, 14),
        ["Calves"] = (4, 8, 14),
        ["Abs"] = (0, 8, 16),
        ["Traps"] = (0, 8, 14),
        ["Forearms"] = (0, 6, 12),
    };

    public static (int Mev, int Mav, int Mrv) GetVolumeLandmarks(string muscle)
    {
        if (VolumeLandmarks.TryGetValue(muscle, out var landmarks))
            return landmarks;

        throw new ArgumentException($"Unknown muscle group: {muscle}", nameof(muscle));
    }

    public static int CalculateWeeklyVolume(string muscle, ProgramGoal goal)
    {
        var (mev, mav, mrv) = GetVolumeLandmarks(muscle);

        return goal switch
        {
            ProgramGoal.Hypertrophy => mav,
            ProgramGoal.Strength => mev + (mav - mev) / 2,
            ProgramGoal.PowerBuilding => mev + (int)((mav - mev) * 0.75),
            ProgramGoal.GeneralFitness => mev + (mav - mev) / 2,
            _ => mav,
        };
    }

    public static IReadOnlyList<string> AllMuscleGroups => VolumeLandmarks.Keys.ToList();
}
