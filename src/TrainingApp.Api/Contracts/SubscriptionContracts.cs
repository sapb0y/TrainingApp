namespace TrainingApp.Api.Contracts;

public record SubscriptionResponse(
    Guid Id,
    string Tier,
    string Status,
    string Interval,
    DateTimeOffset StartDate,
    DateTimeOffset? TrialEndDate,
    DateTimeOffset? CurrentPeriodEnd,
    bool IsTrialExpired,
    DateTimeOffset? CoachFreeUntil,
    bool CoachFreeForever,
    int? TrialDaysRemaining,
    bool HasStripeSubscription,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? PaymentFailedAt);

public record ChangeTierRequest(string Tier);
