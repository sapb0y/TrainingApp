namespace TrainingApp.Core.Entities;

public class MuscleVolumeTarget
{
    public Guid Id { get; set; }
    public Guid ProgramPhaseId { get; set; }
    public ProgramPhase? ProgramPhase { get; set; }
    public required string MuscleGroup { get; set; }
    public int MinSetsPerWeek { get; set; }
    public int TargetSetsPerWeek { get; set; }
    public int MaxSetsPerWeek { get; set; }
}
