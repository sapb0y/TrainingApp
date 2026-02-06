using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Services;

public static class ProgramTemplates
{
    public record TemplateDefinition(
        ProgramTemplate Template,
        string Name,
        int DaysPerWeek,
        IReadOnlyList<WorkoutDayTemplate> Days);

    public record WorkoutDayTemplate(
        string Name,
        WorkoutFocus Focus,
        IReadOnlyList<string> MuscleGroups);

    private static readonly Dictionary<ProgramTemplate, TemplateDefinition> Templates = new()
    {
        [ProgramTemplate.PushPullLegs] = new(
            ProgramTemplate.PushPullLegs,
            "Push/Pull/Legs",
            6,
            [
                new("Push A", WorkoutFocus.Push, ["Chest", "Shoulders", "Triceps"]),
                new("Pull A", WorkoutFocus.Pull, ["Back", "Biceps", "Forearms"]),
                new("Legs A", WorkoutFocus.Legs, ["Quads", "Hamstrings", "Glutes", "Calves"]),
                new("Push B", WorkoutFocus.Push, ["Chest", "Shoulders", "Triceps"]),
                new("Pull B", WorkoutFocus.Pull, ["Back", "Biceps", "Forearms"]),
                new("Legs B", WorkoutFocus.Legs, ["Quads", "Hamstrings", "Glutes", "Calves"]),
            ]),

        [ProgramTemplate.UpperLower] = new(
            ProgramTemplate.UpperLower,
            "Upper/Lower",
            4,
            [
                new("Upper A", WorkoutFocus.Upper, ["Chest", "Back", "Shoulders", "Biceps", "Triceps"]),
                new("Lower A", WorkoutFocus.Lower, ["Quads", "Hamstrings", "Glutes", "Calves"]),
                new("Upper B", WorkoutFocus.Upper, ["Chest", "Back", "Shoulders", "Biceps", "Triceps"]),
                new("Lower B", WorkoutFocus.Lower, ["Quads", "Hamstrings", "Glutes", "Calves"]),
            ]),

        [ProgramTemplate.FullBody] = new(
            ProgramTemplate.FullBody,
            "Full Body",
            3,
            [
                new("Full Body A", WorkoutFocus.FullBody, ["Chest", "Back", "Quads", "Shoulders", "Biceps", "Triceps"]),
                new("Full Body B", WorkoutFocus.FullBody, ["Chest", "Back", "Hamstrings", "Glutes", "Shoulders", "Abs"]),
                new("Full Body C", WorkoutFocus.FullBody, ["Back", "Quads", "Shoulders", "Biceps", "Calves", "Abs"]),
            ]),

        [ProgramTemplate.BroSplit] = new(
            ProgramTemplate.BroSplit,
            "Bro Split",
            5,
            [
                new("Chest", WorkoutFocus.Chest, ["Chest", "Abs"]),
                new("Back", WorkoutFocus.Back, ["Back", "Traps"]),
                new("Shoulders", WorkoutFocus.Shoulders, ["Shoulders", "Abs"]),
                new("Legs", WorkoutFocus.Legs, ["Quads", "Hamstrings", "Glutes", "Calves"]),
                new("Arms", WorkoutFocus.Arms, ["Biceps", "Triceps", "Forearms"]),
            ]),
    };

    public static TemplateDefinition GetTemplate(ProgramTemplate template)
    {
        if (Templates.TryGetValue(template, out var definition))
            return definition;

        throw new ArgumentException($"Unknown template: {template}", nameof(template));
    }

    public static IReadOnlyList<TemplateDefinition> GetAll() => Templates.Values.ToList();
}
