using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using TrainingApp.Core.Configuration;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Infrastructure.External.Stripe;

public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;
    private readonly StripePriceIds _priceIds;

    public StripePaymentService(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
        _priceIds = _settings.PriceIds;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<string> CreateOrGetCustomerAsync(Guid userId, string email, string name, CancellationToken ct = default)
    {
        var service = new CustomerService();
        var searchResult = await service.SearchAsync(new CustomerSearchOptions
        {
            Query = $"metadata['userId']:'{userId}'"
        }, cancellationToken: ct);

        if (searchResult.Data.Count > 0)
            return searchResult.Data[0].Id;

        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
        }, cancellationToken: ct);

        return customer.Id;
    }

    public async Task<CheckoutResult> CreateCheckoutSessionAsync(Guid userId, string stripeCustomerId, SubscriptionTier tier, BillingInterval interval, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var priceId = GetPriceId(tier, interval);

        var service = new SessionService();
        var session = await service.CreateAsync(new SessionCreateOptions
        {
            Customer = stripeCustomerId,
            Mode = "subscription",
            LineItems =
            [
                new SessionLineItemOptions { Price = priceId, Quantity = 1 }
            ],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
                ["tier"] = tier.ToString()
            }
        }, cancellationToken: ct);

        return new CheckoutResult(session.Id, session.Url);
    }

    public async Task<string> CreatePortalSessionAsync(string stripeCustomerId, string returnUrl, CancellationToken ct = default)
    {
        var service = new global::Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(new global::Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl
        }, cancellationToken: ct);

        return session.Url;
    }

    public async Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var service = new SubscriptionService();
        await service.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
        {
            CancelAtPeriodEnd = true
        }, cancellationToken: ct);
    }

    public async Task<string> CreateSetupIntentAsync(string stripeCustomerId, CancellationToken ct = default)
    {
        var service = new SetupIntentService();
        var intent = await service.CreateAsync(new SetupIntentCreateOptions
        {
            Customer = stripeCustomerId,
            PaymentMethodTypes = ["card"],
        }, cancellationToken: ct);

        return intent.ClientSecret;
    }

    public async Task<string> CreateSubscriptionWithTrialAsync(string stripeCustomerId, string priceId, int trialDays, CancellationToken ct = default)
    {
        var service = new SubscriptionService();
        var sub = await service.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = stripeCustomerId,
            Items = [new SubscriptionItemOptions { Price = priceId }],
            TrialPeriodDays = trialDays,
            PaymentBehavior = "default_incomplete",
        }, cancellationToken: ct);

        return sub.Id;
    }

    private string GetPriceId(SubscriptionTier tier, BillingInterval interval) => (tier, interval) switch
    {
        (SubscriptionTier.Athlete, BillingInterval.Monthly) => _priceIds.AthleteMonthly,
        (SubscriptionTier.Athlete, BillingInterval.Annual) => _priceIds.AthleteAnnual,
        (SubscriptionTier.Competitor, BillingInterval.Monthly) => _priceIds.CompetitorMonthly,
        (SubscriptionTier.Competitor, BillingInterval.Annual) => _priceIds.CompetitorAnnual,
        (SubscriptionTier.Coach, BillingInterval.Monthly) => _priceIds.CoachMonthly,
        (SubscriptionTier.Coach, BillingInterval.Annual) => _priceIds.CoachAnnual,
        _ => throw new ArgumentException($"No price for {tier}/{interval}")
    };
}
