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
    public bool IsWarmup { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Estimated 1RM using Brzycki formula: weight * (36 / (37 - reps))
    /// </summary>
    public decimal? CalculateE1Rm()
    {
        if (ActualWeight is null || ActualReps is null || ActualReps < 1 || ActualReps > 36)
            return null;

        return ActualWeight.Value * (36m / (37m - ActualReps.Value));
    }
}
