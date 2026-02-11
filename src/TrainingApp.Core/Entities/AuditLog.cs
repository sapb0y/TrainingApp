namespace TrainingApp.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public AdminAction Action { get; set; }
    public Guid TargetUserId { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum AdminAction
{
    RoleChange,
    TierOverride,
    TrialExtend,
    SubscriptionCancel
}
