namespace TrainingApp.Core.DTOs;

public record AdminStats(
    int TotalUsers,
    int ActiveSubscriptions,
    int TrialCount,
    int PaidCount,
    int PastDueCount,
    int CancelledCount,
    int CoachCount,
    decimal MrrEstimate,
    int RegistrationsLast30Days);

public record AdminUserSummary(
    Guid Id,
    string? Email,
    string? DisplayName,
    string? Role,
    string? Tier,
    string? Status,
    DateTimeOffset CreatedAt);

public record AdminUserDetail(
    Guid Id,
    string? Email,
    string? DisplayName,
    string? Role,
    string? Tier,
    string? SubscriptionStatus,
    DateTimeOffset? TrialEndDate,
    DateTimeOffset? CurrentPeriodEnd,
    string? StripeCustomerId,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? PaymentFailedAt,
    int WorkoutCount,
    DateTimeOffset? LastWorkoutDate,
    int CoachAthleteCount,
    DateTimeOffset CreatedAt);

public record AdminSubscriptionSummary(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string? UserName,
    string Tier,
    string Status,
    string Interval,
    DateTimeOffset? TrialEndDate,
    DateTimeOffset? CurrentPeriodEnd,
    string? StripeCustomerId,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? PaymentFailedAt);

public record PaymentFailureSummary(
    Guid UserId,
    string? Email,
    string? Name,
    string Tier,
    DateTimeOffset? PaymentFailedAt,
    string? StripeCustomerId);

public record ActivityEntry(
    ActivityType Type,
    Guid? UserId,
    string Description,
    DateTimeOffset Timestamp);

public enum ActivityType
{
    Registration,
    SubscriptionChange,
    CoachApplication,
    AdminAction
}

public record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
