using System.Net;
using FluentAssertions;

namespace TrainingApp.Integration.Tests.Endpoints;

public class TierGatingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TierGatingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Athlete_AccessingCharts_Returns403()
    {
        var client = _factory.CreateAthleteClient();

        var response = await client.GetAsync("/api/v1/charts/strength");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Athlete_AccessingGoals_Returns403()
    {
        var client = _factory.CreateAthleteClient();

        var response = await client.GetAsync("/api/v1/goals");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Athlete_AccessingExport_Returns403()
    {
        var client = _factory.CreateAthleteClient();

        var response = await client.GetAsync("/api/v1/export/workouts");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DefaultTestUser_AccessingCharts_Succeeds()
    {
        // Default test user has Coach tier (TestAuthHandler default)
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/charts/strength");

        // May return 200 or other non-403 status (no data is fine)
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Athlete_AccessingWorkouts_Succeeds()
    {
        // Workouts are available to all tiers
        var client = _factory.CreateAthleteClient();

        var response = await client.GetAsync("/api/v1/workouts");

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
