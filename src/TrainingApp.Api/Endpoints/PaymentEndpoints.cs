using Microsoft.AspNetCore.Identity;
using TrainingApp.Api.Contracts;
using TrainingApp.Api.Filters;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payment")
            .WithTags("Payment")
            .RequireAuthorization();

        group.MapPost("/checkout-session", CreateCheckoutSession)
            .WithName("CreateCheckoutSession")
            .WithSummary("Create a Stripe checkout session")
            .WithValidation<CreateCheckoutRequest>();

        group.MapPost("/portal-session", CreatePortalSession)
            .WithName("CreatePortalSession")
            .WithSummary("Create a Stripe customer portal session")
            .WithValidation<CreatePortalRequest>();
    }

    private static async Task<IResult> CreateCheckoutSession(
        CreateCheckoutRequest request,
        ICurrentUserService currentUser,
        IPaymentService paymentService,
        ISubscriptionService subscriptionService,
        ICoachApplicationService coachApplicationService,
        UserManager<User> userManager,
        CancellationToken ct)
    {
        var tier = Enum.Parse<SubscriptionTier>(request.Tier, true);
        var interval = Enum.Parse<BillingInterval>(request.Interval, true);

        // Coach tier requires approved application
        if (tier == SubscriptionTier.Coach)
        {
            var application = await coachApplicationService.GetApplicationAsync(currentUser.UserId, ct);
            if (application?.Status != CoachApplicationStatus.Approved)
                throw new Core.Exceptions.ValidationException("Tier", "Coach tier requires an approved application");
        }

        // Check if payment is required (coach free period skips)
        var requiresPayment = await subscriptionService.RequiresPaymentAsync(currentUser.UserId, tier, ct);
        if (!requiresPayment)
            throw new Core.Exceptions.ValidationException("Tier", "Payment not required — you are in a free period");

        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new Core.Exceptions.NotFoundException("User", currentUser.UserId.ToString());

        var customerId = await paymentService.CreateOrGetCustomerAsync(
            currentUser.UserId, user.Email!, user.DisplayName, ct);

        await subscriptionService.SetStripeCustomerIdAsync(currentUser.UserId, customerId, ct);

        var result = await paymentService.CreateCheckoutSessionAsync(
            currentUser.UserId, customerId, tier, interval, request.SuccessUrl, request.CancelUrl, ct);

        return Results.Ok(new CheckoutSessionResponse(result.SessionId, result.SessionUrl));
    }

    private static async Task<IResult> CreatePortalSession(
        CreatePortalRequest request,
        ICurrentUserService currentUser,
        IPaymentService paymentService,
        ISubscriptionService subscriptionService,
        CancellationToken ct)
    {
        var subscription = await subscriptionService.GetSubscriptionAsync(currentUser.UserId, ct);
        if (subscription?.StripeCustomerId is null)
            throw new Core.Exceptions.ValidationException("Subscription", "No Stripe customer found. Complete a checkout first.");

        var portalUrl = await paymentService.CreatePortalSessionAsync(
            subscription.StripeCustomerId, request.ReturnUrl, ct);

        return Results.Ok(new PortalSessionResponse(portalUrl));
    }
}
