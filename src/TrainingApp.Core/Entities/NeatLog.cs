namespace TrainingApp.Core.Entities;

public class NeatLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly Date { get; set; }
    public int StepCount { get; set; }
    public decimal? EstimatedNeatKcal { get; set; }
    public decimal? CompensationPercent { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
