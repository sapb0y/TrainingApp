namespace TrainingApp.Core.Entities;

public class Insight
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public required string RuleName { get; set; }
    public required string Category { get; set; }
    public required string Severity { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public DateOnly GeneratedDate { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
