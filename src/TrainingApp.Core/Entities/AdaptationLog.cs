namespace TrainingApp.Core.Entities;

public class AdaptationLog
{
    public Guid Id { get; set; }
    public Guid WorkoutId { get; set; }
    public Workout? Workout { get; set; }
    public Guid? WorkoutSetId { get; set; }
    public WorkoutSet? WorkoutSet { get; set; }
    public required string RuleName { get; set; }
    public required string Scope { get; set; }
    public required string InputSummary { get; set; }
    public required string OutputSummary { get; set; }
    public bool WasApplied { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
