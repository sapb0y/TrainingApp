using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Filters;

public class TierRequirementFilter : IEndpointFilter
{
    private readonly SubscriptionTier _requiredTier;

    public TierRequirementFilter(SubscriptionTier requiredTier)
    {
        _requiredTier = requiredTier;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var currentUser = ctx.HttpContext.RequestServices
            .GetRequiredService<ICurrentUserService>();

        if (currentUser.Tier < _requiredTier)
            throw new TierRequiredException(_requiredTier);

        return await next(ctx);
    }
}

public static class TierFilterExtensions
{
    public static RouteHandlerBuilder RequireTier(
        this RouteHandlerBuilder builder, SubscriptionTier tier)
    {
        return builder.AddEndpointFilterFactory((ctx, next) =>
        {
            var filter = new TierRequirementFilter(tier);
            return invCtx => filter.InvokeAsync(invCtx, next);
        });
    }

    public static RouteGroupBuilder RequireTier(
        this RouteGroupBuilder group, SubscriptionTier tier)
    {
        group.AddEndpointFilterFactory((ctx, next) =>
        {
            var filter = new TierRequirementFilter(tier);
            return invCtx => filter.InvokeAsync(invCtx, next);
        });
        return group;
    }
}
