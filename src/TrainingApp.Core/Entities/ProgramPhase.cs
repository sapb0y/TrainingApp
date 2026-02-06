namespace TrainingApp.Core.Entities;

public class ProgramPhase
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public Program? Program { get; set; }
    public required string Name { get; set; }
    public PhaseType Type { get; set; }
    public int OrderIndex { get; set; }
    public int DurationWeeks { get; set; }
    public decimal VolumeMultiplier { get; set; } = 1.0m;
    public decimal IntensityMultiplier { get; set; } = 1.0m;
    public int TargetRir { get; set; } = 2;

    public ICollection<ProgramWorkout> Workouts { get; set; } = [];
    public ICollection<MuscleVolumeTarget> VolumeTargets { get; set; } = [];
}

public enum PhaseType
{
    Accumulation,
    Intensification,
    Deload
}
