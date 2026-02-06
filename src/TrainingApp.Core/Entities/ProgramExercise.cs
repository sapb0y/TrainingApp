namespace TrainingApp.Core.Entities;

public class ProgramExercise
{
    public Guid Id { get; set; }
    public Guid ProgramWorkoutId { get; set; }
    public ProgramWorkout? ProgramWorkout { get; set; }
    public Guid ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }
    public int OrderIndex { get; set; }
    public int TargetSets { get; set; }
    public int TargetRepsMin { get; set; }
    public int TargetRepsMax { get; set; }
    public decimal? IntensityPercentage { get; set; }
    public decimal? TargetRpe { get; set; }
    public int RestSeconds { get; set; } = 120;
    public string? Notes { get; set; }
    public string? SupersetGroup { get; set; }
}
