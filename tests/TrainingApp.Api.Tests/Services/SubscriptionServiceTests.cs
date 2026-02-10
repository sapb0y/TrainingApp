using FluentAssertions;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Services;

namespace TrainingApp.Api.Tests.Services;

public class SubscriptionServiceTests
{
    [Fact]
    public void IsTrialExpired_ActiveTrial_ReturnsFalse()
    {
        var service = CreateService();
        var subscription = new UserSubscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(15)
        };

        service.IsTrialExpired(subscription).Should().BeFalse();
    }

    [Fact]
    public void IsTrialExpired_ExpiredTrial_ReturnsTrue()
    {
        var service = CreateService();
        var subscription = new UserSubscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        service.IsTrialExpired(subscription).Should().BeTrue();
    }

    [Fact]
    public void IsTrialExpired_ActiveSubscription_ReturnsFalse()
    {
        var service = CreateService();
        var subscription = new UserSubscription
        {
            Status = SubscriptionStatus.Active,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(-1) // expired but status is Active
        };

        service.IsTrialExpired(subscription).Should().BeFalse();
    }

    [Fact]
    public void IsTrialExpired_NoTrialEndDate_ReturnsFalse()
    {
        var service = CreateService();
        var subscription = new UserSubscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndDate = null
        };

        service.IsTrialExpired(subscription).Should().BeFalse();
    }

    private static SubscriptionService CreateService()
    {
        // SubscriptionService needs DbContext for DB methods, but IsTrialExpired is pure logic
        return new SubscriptionService(null!);
    }
}
