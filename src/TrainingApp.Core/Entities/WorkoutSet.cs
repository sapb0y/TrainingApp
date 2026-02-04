namespace TrainingApp.Core.Entities;

public class WorkoutSet
{
    public Guid Id { get; set; }
    public Guid WorkoutId { get; set; }
    public Workout? Workout { get; set; }
    public Guid ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    public int SetNumber { get; set; }
    public int? TargetReps { get; set; }
    public decimal? TargetWeight { get; set; }
    public int? ActualReps { get; set; }
    public decimal? ActualWeight { get; set; }
    public decimal? Rpe { get; set; }
    public int? Rir { get; set; }
    public bool IsWarmup { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? PerformedAt { get; set; }

    /// <summary>
    /// Estimated 1RM using Epley formula: weight * (1 + reps/30)
    /// </summary>
    public decimal? EstimatedOneRepMax =>
        ActualWeight is null || ActualReps is null || ActualReps < 1 || ActualReps > 30
            ? null
            : ActualWeight.Value * (1 + ActualReps.Value / 30m);
}
