namespace TrainingApp.Core.Entities;

public class Workout
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Name { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public WorkoutStatus Status { get; set; } = WorkoutStatus.Planned;

    public ICollection<WorkoutSet> Sets { get; set; } = [];
}

public enum WorkoutStatus
{
    Planned,
    InProgress,
    Completed,
    Skipped
}
