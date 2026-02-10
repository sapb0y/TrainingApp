namespace TrainingApp.Core.Entities;

public class UserSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public SubscriptionTier Tier { get; set; }
    public SubscriptionStatus Status { get; set; }
    public BillingInterval Interval { get; set; } = BillingInterval.Monthly;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? TrialEndDate { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public DateTimeOffset? CoachFreeUntil { get; set; }
    public bool CoachFreeForever { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum BillingInterval { Monthly, Annual }
