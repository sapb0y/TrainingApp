namespace TrainingApp.Core.Entities;

public class GoalCheckpoint
{
    public Guid Id { get; set; }
    public Guid GoalId { get; set; }
    public Goal? Goal { get; set; }
    public DateOnly Date { get; set; }
    public decimal Value { get; set; }
    public decimal? PercentComplete { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
