using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using TrainingApp.Core.Configuration;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Infrastructure.External.Stripe;

public class StripeWebhookHandler : IStripeWebhookHandler
{
    private readonly string _webhookSecret;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IEmailService _emailService;
    private readonly ILogger<StripeWebhookHandler> _logger;

    public StripeWebhookHandler(
        IOptions<StripeSettings> settings,
        ISubscriptionService subscriptionService,
        IEmailService emailService,
        ILogger<StripeWebhookHandler> logger)
    {
        _webhookSecret = settings.Value.WebhookSecret;
        _subscriptionService = subscriptionService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(string payload, string signatureHeader, CancellationToken ct = default)
    {
        var stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, _webhookSecret);

        _logger.LogInformation("Processing Stripe event {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutCompleted(stripeEvent, ct);
                break;
            case EventTypes.CustomerSubscriptionCreated:
                await HandleSubscriptionCreated(stripeEvent, ct);
                break;
            case EventTypes.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdated(stripeEvent, ct);
                break;
            case EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent, ct);
                break;
            case EventTypes.InvoicePaymentFailed:
                await HandlePaymentFailed(stripeEvent, ct);
                break;
            case EventTypes.InvoicePaid:
                await HandleInvoicePaid(stripeEvent, ct);
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as global::Stripe.Checkout.Session;
        if (session is null) return;

        if (session.Metadata.TryGetValue("userId", out var userIdStr) && Guid.TryParse(userIdStr, out var userId))
        {
            await _subscriptionService.SetStripeCustomerIdAsync(userId, session.CustomerId, ct);
        }
    }

    private async Task HandleSubscriptionCreated(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var firstItem = subscription.Items.Data.FirstOrDefault();
        var priceId = firstItem?.Price?.Id;
        if (priceId is null) return;

        var periodEnd = firstItem?.CurrentPeriodEnd ?? DateTimeOffset.UtcNow.AddMonths(1);

        await _subscriptionService.ActivateFromPaymentAsync(
            subscription.CustomerId,
            subscription.Id,
            priceId,
            periodEnd,
            ct);

        var sub = await _subscriptionService.GetByStripeCustomerIdAsync(subscription.CustomerId, ct);
        if (sub?.User is not null)
        {
            await _emailService.SendSubscriptionConfirmedAsync(
                sub.User.Email!,
                sub.User.DisplayName,
                sub.Tier.ToString(),
                ct);
        }
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        var periodEnd = subscription.Items.Data.FirstOrDefault()?.CurrentPeriodEnd
            ?? DateTimeOffset.UtcNow.AddMonths(1);

        var status = MapStripeStatus(subscription.Status);
        await _subscriptionService.UpdatePeriodAsync(
            subscription.Id,
            periodEnd,
            status,
            ct);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, CancellationToken ct)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription is null) return;

        await _subscriptionService.SetCancelledAsync(subscription.Id, ct);
    }

    private async Task HandlePaymentFailed(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId is null) return;

        await _subscriptionService.SetPaymentFailedAsync(subscriptionId, ct);

        if (invoice!.CustomerEmail is not null)
        {
            await _emailService.SendPaymentFailedAsync(
                invoice.CustomerEmail,
                invoice.CustomerName ?? "Customer",
                ct);
        }
    }

    private async Task HandleInvoicePaid(Event stripeEvent, CancellationToken ct)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var subscriptionId = invoice?.Parent?.SubscriptionDetails?.SubscriptionId;
        if (subscriptionId is null) return;

        var sub = await _subscriptionService.GetByStripeSubscriptionIdAsync(subscriptionId, ct);
        if (sub is not null)
        {
            await _subscriptionService.UpdatePeriodAsync(
                subscriptionId,
                sub.CurrentPeriodEnd ?? DateTimeOffset.UtcNow.AddMonths(1),
                SubscriptionStatus.Active,
                ct);
        }
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus) => stripeStatus switch
    {
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Cancelled,
        "unpaid" => SubscriptionStatus.PastDue,
        _ => SubscriptionStatus.Active
    };
}
