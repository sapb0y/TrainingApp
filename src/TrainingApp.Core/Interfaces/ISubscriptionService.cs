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
}
