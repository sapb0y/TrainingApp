namespace TrainingApp.Core.Entities;

public class CoachNote
{
    public Guid Id { get; set; }
    public Guid CoachAthleteId { get; set; }
    public CoachAthlete? CoachAthlete { get; set; }
    public Guid CoachId { get; set; }
    public Guid AthleteId { get; set; }
    public Guid? WorkoutId { get; set; }
    public Workout? Workout { get; set; }
    public Guid? WorkoutSetId { get; set; }
    public WorkoutSet? WorkoutSet { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
