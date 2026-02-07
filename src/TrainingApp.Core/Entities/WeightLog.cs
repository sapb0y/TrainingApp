namespace TrainingApp.Core.Entities;

public class WeightLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly Date { get; set; }
    public decimal WeightKg { get; set; }
    public decimal? MovingAverage7d { get; set; }
    public decimal? MovingAverage30d { get; set; }
    public decimal? WeeklyRateKg { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
