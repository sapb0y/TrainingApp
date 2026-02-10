using TrainingApp.Core.Entities;

namespace TrainingApp.Core.Interfaces;

public record CheckoutResult(string SessionId, string SessionUrl);

public interface IPaymentService
{
    Task<string> CreateOrGetCustomerAsync(Guid userId, string email, string name, CancellationToken ct = default);
    Task<CheckoutResult> CreateCheckoutSessionAsync(Guid userId, string stripeCustomerId, SubscriptionTier tier, BillingInterval interval, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task<string> CreatePortalSessionAsync(string stripeCustomerId, string returnUrl, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken ct = default);
}
