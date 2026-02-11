using FluentAssertions;
using TrainingApp.Core.Configuration;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Services;

namespace TrainingApp.Api.Tests.Services;

public class SubscriptionServiceExtendedTests
{
    private static SubscriptionService CreateService()
    {
        var settings = Microsoft.Extensions.Options.Options.Create(new StripeSettings());
        return new SubscriptionService(null!, settings);
    }

    [Fact]
    public void GetTrialDaysRemaining_ActiveTrial_ReturnsCorrectDays()
    {
        var service = CreateService();
        var sub = new UserSubscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(15)
        };

        var result = service.GetTrialDaysRemaining(sub);

        // .Days truncates — AddDays(15) yields 14 or 15 depending on sub-second timing
        result.Should().BeGreaterThanOrEqualTo(14);
        result.Should().BeLessThanOrEqualTo(15);
    }

    [Fact]
    public void GetTrialDaysRemaining_ExpiredTrial_ReturnsZero()
    {
        var service = CreateService();
        var sub = new UserSubscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var result = service.GetTrialDaysRemaining(sub);

        result.Should().Be(0);
    }

    [Fact]
    public void GetTrialDaysRemaining_ActiveStatus_ReturnsNull()
    {
        var service = CreateService();
        var sub = new UserSubscription
        {
            Status = SubscriptionStatus.Active,
            TrialEndDate = DateTimeOffset.UtcNow.AddDays(10)
        };

        var result = service.GetTrialDaysRemaining(sub);

        result.Should().BeNull();
    }

    [Fact]
    public void GetTrialDaysRemaining_NoEndDate_ReturnsNull()
    {
        var service = CreateService();
        var sub = new UserSubscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndDate = null
        };

        var result = service.GetTrialDaysRemaining(sub);

        result.Should().BeNull();
    }

    [Fact]
    public void GetTrialDaysRemaining_ExactlyToday_ReturnsZero()
    {
        var service = CreateService();
        var sub = new UserSubscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndDate = DateTimeOffset.UtcNow // expires now
        };

        var result = service.GetTrialDaysRemaining(sub);

        result.Should().Be(0);
    }
}
