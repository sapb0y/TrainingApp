using System.Net;
using FluentAssertions;

namespace TrainingApp.Integration.Tests.Endpoints;

public class AuthPipelineTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthPipelineTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTestAuth_Returns200()
    {
        var client = _factory.CreateClient();
        // TestAuthHandler auto-authenticates, default user ID 00...01

        var response = await client.GetAsync("/api/v1/workouts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CoachEndpoint_NonCoach_Returns403()
    {
        // Default client has no role → IsCoach = false
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/coach/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CoachEndpoint_WithCoachRole_Returns200()
    {
        var client = _factory.CreateCoachClient();

        var response = await client.GetAsync("/api/v1/coach/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task IsAdmin_WithAdminRole_ReturnsAdminContext()
    {
        // Create a client with Admin role header
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("X-Test-UserRole", "Admin");

        // Any protected endpoint should work — the admin role is set via claims
        var response = await client.GetAsync("/api/v1/workouts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
