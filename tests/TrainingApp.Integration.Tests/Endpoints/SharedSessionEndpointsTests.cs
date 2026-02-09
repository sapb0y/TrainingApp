using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class SharedSessionEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _partnerClient;

    public SharedSessionEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _partnerClient = factory.CreatePartnerClient();
    }

    private async Task<Guid> CreateActivePartnership()
    {
        var inviteResp = await _client.PostAsync("/api/v1/partners/invite", null);
        var invite = await inviteResp.Content.ReadFromJsonAsync<CreateInviteResponse>();
        await _partnerClient.PostAsJsonAsync("/api/v1/partners/accept",
            new AcceptInviteRequest(invite!.InviteCode));
        return invite.PartnershipId;
    }

    [Fact]
    public async Task CreateSession_Returns200()
    {
        var partnershipId = await CreateActivePartnership();

        var response = await _client.PostAsJsonAsync("/api/v1/shared-sessions",
            new CreateSharedSessionRequest(partnershipId, "2026-04-01", null, null, "Leg day together"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SharedSessionResponse>();
        result.Should().NotBeNull();
        result!.ScheduledDate.Should().Be("2026-04-01");
        result.Status.Should().Be("Planned");
    }

    [Fact]
    public async Task GetSession_Returns200()
    {
        var partnershipId = await CreateActivePartnership();
        var createResp = await _client.PostAsJsonAsync("/api/v1/shared-sessions",
            new CreateSharedSessionRequest(partnershipId, "2026-04-02", null, null, null));
        var created = await createResp.Content.ReadFromJsonAsync<SharedSessionResponse>();

        var response = await _client.GetAsync($"/api/v1/shared-sessions/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SharedSessionResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task ListSessions_ByPartnership_Returns200()
    {
        var partnershipId = await CreateActivePartnership();
        await _client.PostAsJsonAsync("/api/v1/shared-sessions",
            new CreateSharedSessionRequest(partnershipId, "2026-04-03", null, null, null));

        var response = await _client.GetAsync($"/api/v1/shared-sessions?partnershipId={partnershipId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SharedSessionListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateSchedule_Returns200()
    {
        var partnershipId = await CreateActivePartnership();
        var createResp = await _client.PostAsJsonAsync("/api/v1/shared-sessions",
            new CreateSharedSessionRequest(partnershipId, "2026-04-04", null, null, null));
        var created = await createResp.Content.ReadFromJsonAsync<SharedSessionResponse>();

        var response = await _client.PostAsync($"/api/v1/shared-sessions/{created!.Id}/generate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelSession_Returns200_StatusCancelled()
    {
        var partnershipId = await CreateActivePartnership();
        var createResp = await _client.PostAsJsonAsync("/api/v1/shared-sessions",
            new CreateSharedSessionRequest(partnershipId, "2026-04-05", null, null, null));
        var created = await createResp.Content.ReadFromJsonAsync<SharedSessionResponse>();

        var response = await _client.PostAsync($"/api/v1/shared-sessions/{created!.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SharedSessionResponse>();
        result!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CreateSession_NonMember_ReturnsNotFound()
    {
        // Create partnership between client and partner, then try with a different ID
        var partnershipId = await CreateActivePartnership();

        // End the partnership first so we can create a fresh one
        await _client.PostAsync($"/api/v1/partners/{partnershipId}/end", null);

        // Now create a new one and try to create session with old ended partnership
        var response = await _client.PostAsJsonAsync("/api/v1/shared-sessions",
            new CreateSharedSessionRequest(partnershipId, "2026-04-06", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
