using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/subscription")
            .WithTags("Subscription")
            .RequireAuthorization();

        group.MapGet("/", GetSubscription)
            .WithName("GetSubscription")
            .WithSummary("Get current subscription info");

        group.MapPost("/change", ChangeTier)
            .WithName("ChangeTier")
            .WithSummary("Change subscription tier");
    }

    private static async Task<IResult> GetSubscription(
        ICurrentUserService currentUser,
        ISubscriptionService subscriptionService,
        CancellationToken ct)
    {
        var subscription = await subscriptionService.GetSubscriptionAsync(currentUser.UserId, ct);

        if (subscription is null)
            return Results.Ok(new { tier = "Athlete", status = "None" });

        return Results.Ok(new SubscriptionResponse(
            subscription.Id,
            subscription.Tier.ToString(),
            subscription.Status.ToString(),
            subscription.Interval.ToString(),
            subscription.StartDate,
            subscription.TrialEndDate,
            subscription.CurrentPeriodEnd,
            subscriptionService.IsTrialExpired(subscription),
            subscription.CoachFreeUntil,
            subscription.CoachFreeForever));
    }

    private static async Task<IResult> ChangeTier(
        ChangeTierRequest request,
        ICurrentUserService currentUser,
        ISubscriptionService subscriptionService,
        CancellationToken ct)
    {
        if (!Enum.TryParse<SubscriptionTier>(request.Tier, true, out var tier))
            throw new Core.Exceptions.ValidationException("Tier", $"Invalid tier: {request.Tier}");

        if (tier == SubscriptionTier.Coach)
            throw new Core.Exceptions.ValidationException("Tier", "Coach tier requires application approval");

        var subscription = await subscriptionService.ChangeTierAsync(currentUser.UserId, tier, ct);

        return Results.Ok(new SubscriptionResponse(
            subscription.Id,
            subscription.Tier.ToString(),
            subscription.Status.ToString(),
            subscription.Interval.ToString(),
            subscription.StartDate,
            subscription.TrialEndDate,
            subscription.CurrentPeriodEnd,
            subscriptionService.IsTrialExpired(subscription),
            subscription.CoachFreeUntil,
            subscription.CoachFreeForever));
    }
}
