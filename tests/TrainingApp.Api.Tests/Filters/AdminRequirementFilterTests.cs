using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Exceptions;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Tests.Filters;

public class AdminRequirementFilterTests
{
    [Fact]
    public async Task NonAdmin_ThrowsForbidden()
    {
        var currentUser = new FakeCurrentUserService(isAdmin: false);
        var filter = new Api.Filters.AdminRequirementFilter();
        var ctx = CreateFilterContext(currentUser);

        Func<Task> act = async () => await filter.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>(null));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Admin_Succeeds()
    {
        var currentUser = new FakeCurrentUserService(isAdmin: true);
        var filter = new Api.Filters.AdminRequirementFilter();
        var ctx = CreateFilterContext(currentUser);

        var result = await filter.InvokeAsync(ctx, _ => ValueTask.FromResult<object?>("ok"));

        result.Should().Be("ok");
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
        public FakeCurrentUserService(bool isAdmin) => IsAdmin = isAdmin;
        public Guid UserId => Guid.NewGuid();
        public bool IsAuthenticated => true;
        public bool IsCoach => false;
        public bool IsAdmin { get; }
        public SubscriptionTier Tier => SubscriptionTier.Coach;
    }
}
