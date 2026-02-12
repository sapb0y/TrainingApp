namespace TrainingApp.Core.Entities;

public class CoachMessage
{
    public Guid Id { get; set; }
    public Guid CoachAthleteId { get; set; }
    public CoachAthlete? CoachAthlete { get; set; }
    public Guid SenderId { get; set; }
    public User? Sender { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
