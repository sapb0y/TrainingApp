using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.DTOs;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace TrainingApp.Integration.Tests.Endpoints;

public class AdminDashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _normalClient;

    public AdminDashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAdminClient();
        _normalClient = factory.CreateClient();
    }

    [Fact]
    public async Task GetStats_Admin_Returns200()
    {
        var response = await _adminClient.GetAsync("/api/v1/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<AdminStats>();
        stats!.TotalUsers.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetStats_NonAdmin_Returns403()
    {
        var response = await _normalClient.GetAsync("/api/v1/admin/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_Admin_ReturnsPaginatedResults()
    {
        var response = await _adminClient.GetAsync("/api/v1/admin/users?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminUserSummary>>();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetSubscriptions_Admin_ReturnsList()
    {
        // Seed a subscription first
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        if (!db.UserSubscriptions.Any(s => s.UserId == userId))
        {
            db.UserSubscriptions.Add(new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Tier = SubscriptionTier.Competitor,
                Status = SubscriptionStatus.Trial,
                StartDate = DateTimeOffset.UtcNow,
                TrialEndDate = DateTimeOffset.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();
        }

        var response = await _adminClient.GetAsync("/api/v1/admin/subscriptions?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExtendTrial_Admin_Returns200()
    {
        // Ensure subscription exists
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var sub = db.UserSubscriptions.FirstOrDefault(s => s.UserId == userId);
        if (sub is null)
        {
            sub = new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Tier = SubscriptionTier.Competitor,
                Status = SubscriptionStatus.Trial,
                StartDate = DateTimeOffset.UtcNow,
                TrialEndDate = DateTimeOffset.UtcNow.AddDays(30)
            };
            db.UserSubscriptions.Add(sub);
            await db.SaveChangesAsync();
        }

        var request = new ExtendTrialRequest(7);
        var response = await _adminClient.PostAsJsonAsync($"/api/v1/admin/subscriptions/{sub.Id}/extend", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPaymentFailures_Admin_Returns200()
    {
        var response = await _adminClient.GetAsync("/api/v1/admin/payments/failures?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
