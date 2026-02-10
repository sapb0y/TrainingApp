using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Tests.Filters;

public class TierRequirementFilterTests
{
    [Fact]
    public async Task Athlete_AccessingCompetitorFeature_ThrowsTierRequired()
    {
        var currentUser = CreateCurrentUser(SubscriptionTier.Athlete);

        var filter = new Api.Filters.TierRequirementFilter(SubscriptionTier.Competitor);
        var ctx = CreateFilterContext(currentUser);

        Func<Task> act = async () => await filter.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>(null));

        var ex = await act.Should().ThrowAsync<TierRequiredException>();
        ex.And.RequiredTier.Should().Be(SubscriptionTier.Competitor);
    }

    [Fact]
    public async Task Competitor_AccessingCompetitorFeature_Succeeds()
    {
        var currentUser = CreateCurrentUser(SubscriptionTier.Competitor);

        var filter = new Api.Filters.TierRequirementFilter(SubscriptionTier.Competitor);
        var ctx = CreateFilterContext(currentUser);

        var result = await filter.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("ok"));

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Coach_AccessingCompetitorFeature_Succeeds()
    {
        var currentUser = CreateCurrentUser(SubscriptionTier.Coach);

        var filter = new Api.Filters.TierRequirementFilter(SubscriptionTier.Competitor);
        var ctx = CreateFilterContext(currentUser);

        var result = await filter.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("ok"));

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Competitor_AccessingCoachFeature_ThrowsTierRequired()
    {
        var currentUser = CreateCurrentUser(SubscriptionTier.Competitor);

        var filter = new Api.Filters.TierRequirementFilter(SubscriptionTier.Coach);
        var ctx = CreateFilterContext(currentUser);

        Func<Task> act = async () => await filter.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>(null));

        var ex = await act.Should().ThrowAsync<TierRequiredException>();
        ex.And.RequiredTier.Should().Be(SubscriptionTier.Coach);
    }

    private static ICurrentUserService CreateCurrentUser(SubscriptionTier tier)
    {
        return new FakeCurrentUserService(tier);
    }

    private static EndpointFilterInvocationContext CreateFilterContext(ICurrentUserService currentUser)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(currentUser)
            .BuildServiceProvider();

        return new DefaultEndpointFilterInvocationContext(httpContext);
    }

    private class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(SubscriptionTier tier) => Tier = tier;
        public Guid UserId => Guid.NewGuid();
        public bool IsAuthenticated => true;
        public bool IsCoach => false;
        public bool IsAdmin => false;
        public SubscriptionTier Tier { get; }
    }
}
