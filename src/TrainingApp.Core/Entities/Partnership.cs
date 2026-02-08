namespace TrainingApp.Core.Entities;

public class Partnership
{
    public Guid Id { get; set; }
    public Guid RequesterId { get; set; }
    public User? Requester { get; set; }
    public Guid? ResponderId { get; set; }
    public User? Responder { get; set; }
    public required string InviteCode { get; set; }
    public PartnershipStatus Status { get; set; } = PartnershipStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public ICollection<SharedSession> SharedSessions { get; set; } = [];
}

public enum PartnershipStatus { Pending, Active, Ended }
