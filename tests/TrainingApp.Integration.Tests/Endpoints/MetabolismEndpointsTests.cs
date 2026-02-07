using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests.Endpoints;

public class MetabolismEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public MetabolismEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_Returns200_WithDefaults_WhenNoProfile()
    {
        var response = await _client.GetAsync("/api/v1/metabolism/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MetabolismSummaryResponse>();
        result.Should().NotBeNull();
        // Without sex/height/DOB, BMR should be null
        result!.BmrKcal.Should().BeNull();
    }

    [Fact]
    public async Task GetSummary_Returns200_WithPopulatedFields_WhenDataExists()
    {
        // Set up user profile
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Id == TestUserId);
            user.Settings.Sex = BiologicalSex.Male;
            user.Settings.HeightCm = 180m;
            user.Settings.DateOfBirth = new DateOnly(1995, 1, 1);
            user.Settings.ActivityLevel = ActivityLevel.Moderate;
            await db.SaveChangesAsync();
        }

        // Log weight
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(today, 80.0m, null));

        var response = await _client.GetAsync("/api/v1/metabolism/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MetabolismSummaryResponse>();
        result.Should().NotBeNull();
        result!.BmrKcal.Should().BeGreaterThan(0);
        result.TdeeKcal.Should().BeGreaterThan(0);
        result.CurrentWeightKg.Should().Be(80.0m);
        result.Projections.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSummary_IncludesAlerts_WhenRulesFire()
    {
        // Set up user profile
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            var user = await db.Users.FirstAsync(u => u.Id == TestUserId);
            user.Settings.Sex = BiologicalSex.Male;
            user.Settings.HeightCm = 180m;
            user.Settings.DateOfBirth = new DateOnly(1995, 1, 1);
            user.Settings.ActivityLevel = ActivityLevel.Moderate;

            // Clear any active deficits first
            var active = await db.DeficitPhases
                .Where(d => d.UserId == TestUserId && (d.Status == DeficitPhaseStatus.Active || d.Status == DeficitPhaseStatus.Paused))
                .ToListAsync();
            foreach (var d in active)
            {
                d.Status = DeficitPhaseStatus.Completed;
                d.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }
            await db.SaveChangesAsync();
        }

        // Log weight with rapid loss rate (simulate via WeeklyRateKg)
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-60);
        await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(date.ToString("yyyy-MM-dd"), 90.0m, null));

        // Start a deficit that started long ago
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            db.DeficitPhases.Add(new DeficitPhase
            {
                Id = Guid.NewGuid(),
                UserId = TestUserId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90),
                StartWeightKg = 95.0m,
                WeeklyRateKg = 0.5m,
                Strategy = DeficitStrategy.Moderate,
                Status = DeficitPhaseStatus.Active,
                DietBreakIntervalWeeks = 6,
            });
            await db.SaveChangesAsync();
        }

        // Log current weight much lower to trigger rapid loss
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(today, 80.0m, null));

        var response = await _client.GetAsync("/api/v1/metabolism/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MetabolismSummaryResponse>();
        result.Should().NotBeNull();
        // Should have at least diet break due alert (90 days > 42 day interval)
        result!.Alerts.Should().NotBeEmpty();
    }
}
