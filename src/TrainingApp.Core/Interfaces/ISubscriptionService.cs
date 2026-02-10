using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public interface ISubscriptionService
{
    Task<UserSubscription> CreateTrialAsync(Guid userId, CancellationToken ct = default);
    Task<UserSubscription?> GetSubscriptionAsync(Guid userId, CancellationToken ct = default);
    Task<UserSubscription> ChangeTierAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default);
    Task<SubscriptionTier> GetEffectiveTierAsync(Guid userId, CancellationToken ct = default);
    bool IsTrialExpired(UserSubscription subscription);
    Task CheckCoachClientThresholdAsync(Guid userId, CancellationToken ct = default);

    // Webhook-driven methods
    Task ActivateFromPaymentAsync(string stripeCustomerId, string stripeSubscriptionId, string stripePriceId, DateTimeOffset currentPeriodEnd, CancellationToken ct = default);
    Task SetPaymentFailedAsync(string stripeSubscriptionId, CancellationToken ct = default);
    Task SetCancelledAsync(string stripeSubscriptionId, CancellationToken ct = default);
    Task UpdatePeriodAsync(string stripeSubscriptionId, DateTimeOffset currentPeriodEnd, SubscriptionStatus status, CancellationToken ct = default);
    Task<UserSubscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken ct = default);
    Task<UserSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken ct = default);
    Task<bool> RequiresPaymentAsync(Guid userId, SubscriptionTier tier, CancellationToken ct = default);
    Task SetStripeCustomerIdAsync(Guid userId, string stripeCustomerId, CancellationToken ct = default);
    int? GetTrialDaysRemaining(UserSubscription subscription);
}
