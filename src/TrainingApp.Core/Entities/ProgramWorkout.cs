namespace TrainingApp.Core.Entities;

public class ProgramWorkout
{
    public Guid Id { get; set; }
    public Guid ProgramPhaseId { get; set; }
    public ProgramPhase? ProgramPhase { get; set; }
    public required string Name { get; set; }
    public WorkoutFocus Focus { get; set; }
    public int DayNumber { get; set; }
    public int OrderIndex { get; set; }

    public ICollection<ProgramExercise> Exercises { get; set; } = [];
}

public enum WorkoutFocus
{
    Push,
    Pull,
    Legs,
    Upper,
    Lower,
    FullBody,
    Chest,
    Back,
    Shoulders,
    Arms
}
