namespace TrainingApp.Core.Entities;

public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string Title { get; set; }
    public GoalType Type { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public decimal? TargetValue { get; set; }
    public string? TargetUnit { get; set; }
    public Guid? ExerciseId { get; set; }
    public decimal? StartValue { get; set; }
    public DateOnly CreatedDate { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateOnly? AchievedDate { get; set; }
    public string? Notes { get; set; }
    public ICollection<GoalCheckpoint> Checkpoints { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum GoalType { Strength, BodyWeight, Cardio, Custom }
public enum GoalStatus { Active, Achieved, Abandoned }
