namespace TrainingApp.Core.Entities;

public class CardioSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public DateOnly Date { get; set; }
    public CardioModality Modality { get; set; }
    public CardioIntensityZone Zone { get; set; }
    public int DurationMinutes { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? AverageHeartRate { get; set; }
    public int? MaxHeartRate { get; set; }
    public decimal? CardioTrimp { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
