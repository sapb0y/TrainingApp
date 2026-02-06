namespace TrainingApp.Core.Entities;

public class Exercise
{
    public Guid Id { get; set; }
    public int? WgerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Category { get; set; }
    public List<string> PrimaryMuscles { get; set; } = [];
    public List<string> SecondaryMuscles { get; set; } = [];
    public List<string> Equipment { get; set; } = [];
    public bool IsCustom { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? CachedAt { get; set; }

    public ICollection<WorkoutSet> WorkoutSets { get; set; } = [];
    public ICollection<ProgramExercise> ProgramExercises { get; set; } = [];
}
