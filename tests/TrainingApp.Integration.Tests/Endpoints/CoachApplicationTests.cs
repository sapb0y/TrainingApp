using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class CoachApplicationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CoachApplicationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitApplication_ValidRequest_ReturnsCreated()
    {
        var request = new SubmitCoachApplicationRequest(
            "CSCS certified, 5 years experience", 15, "Scale my coaching business", null);

        var response = await _client.PostAsJsonAsync("/api/v1/coach-applications", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CoachApplicationResponse>();
        result!.Status.Should().Be("Pending");
        result.Credentials.Should().Be("CSCS certified, 5 years experience");
    }

    [Fact]
    public async Task GetMyApplication_AfterSubmit_ReturnsApplication()
    {
        // Use partner user to avoid conflict with other tests
        var client = _factory.CreatePartnerClient();
        var request = new SubmitCoachApplicationRequest(
            "ACE certified", 8, "Grow online coaching", "Website: example.com");

        await client.PostAsJsonAsync("/api/v1/coach-applications", request);

        var response = await client.GetAsync("/api/v1/coach-applications/mine");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachApplicationResponse>();
        result!.Credentials.Should().Be("ACE certified");
    }

    [Fact]
    public async Task GetMyApplication_NoApplication_Returns404()
    {
        // Coach user hasn't submitted an application
        var client = _factory.CreateCoachClient();

        var response = await client.GetAsync("/api/v1/coach-applications/mine");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminGetPending_NonAdmin_Returns403()
    {
        // Default test user is not admin
        var response = await _client.GetAsync("/api/v1/admin/coach-applications");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
