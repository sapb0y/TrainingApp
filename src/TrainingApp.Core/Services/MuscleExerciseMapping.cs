namespace TrainingApp.Core.Services;

public static class MuscleExerciseMapping
{
    private static readonly Dictionary<string, IReadOnlyList<string>> SearchTerms = new()
    {
        ["Chest"] = ["Pectoralis major", "Chest"],
        ["Back"] = ["Latissimus dorsi", "Back"],
        ["Shoulders"] = ["Deltoid", "Shoulders"],
        ["Quads"] = ["Quadriceps", "Legs"],
        ["Hamstrings"] = ["Hamstrings", "Biceps femoris"],
        ["Glutes"] = ["Gluteus maximus", "Glutes"],
        ["Biceps"] = ["Biceps brachii", "Biceps"],
        ["Triceps"] = ["Triceps brachii", "Triceps"],
        ["Calves"] = ["Gastrocnemius", "Calves"],
        ["Abs"] = ["Rectus abdominis", "Abs"],
        ["Traps"] = ["Trapezius", "Traps"],
        ["Forearms"] = ["Brachioradialis", "Forearms"],
    };

    public static IReadOnlyList<string> GetSearchTerms(string muscleGroup)
    {
        if (SearchTerms.TryGetValue(muscleGroup, out var terms))
            return terms;

        throw new ArgumentException($"Unknown muscle group: {muscleGroup}", nameof(muscleGroup));
    }

    public static IReadOnlyList<string> AllMuscleGroups => SearchTerms.Keys.ToList();
}
