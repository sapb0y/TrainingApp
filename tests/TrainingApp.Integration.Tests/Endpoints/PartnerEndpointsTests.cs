using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class PartnerEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _partnerClient;

    public PartnerEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _partnerClient = factory.CreatePartnerClient();
    }

    [Fact]
    public async Task CreateInvite_Returns200_WithCode()
    {
        var response = await _client.PostAsync("/api/v1/partners/invite", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateInviteResponse>();
        result.Should().NotBeNull();
        result!.InviteCode.Should().HaveLength(6);
        result.PartnershipId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AcceptInvite_ValidCode_ReturnsActivePartnership()
    {
        // User A creates invite
        var inviteResponse = await _client.PostAsync("/api/v1/partners/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CreateInviteResponse>();

        // User B accepts
        var response = await _partnerClient.PostAsJsonAsync("/api/v1/partners/accept",
            new AcceptInviteRequest(invite!.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PartnershipResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Active");
        result.ResponderId.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptInvite_SelfInvite_Returns400()
    {
        var inviteResponse = await _client.PostAsync("/api/v1/partners/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CreateInviteResponse>();

        // Same user tries to accept own invite
        var response = await _client.PostAsJsonAsync("/api/v1/partners/accept",
            new AcceptInviteRequest(invite!.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeclineInvite_Returns200()
    {
        var inviteResponse = await _client.PostAsync("/api/v1/partners/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CreateInviteResponse>();

        var response = await _partnerClient.PostAsJsonAsync("/api/v1/partners/decline",
            new DeclineInviteRequest(invite!.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetActivePartnership_AfterAccept_ReturnsPartnership()
    {
        var inviteResponse = await _client.PostAsync("/api/v1/partners/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CreateInviteResponse>();

        await _partnerClient.PostAsJsonAsync("/api/v1/partners/accept",
            new AcceptInviteRequest(invite!.InviteCode));

        var response = await _client.GetAsync("/api/v1/partners/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PartnershipResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Active");
    }

    [Fact]
    public async Task EndPartnership_Returns200_StatusEnded()
    {
        var inviteResponse = await _client.PostAsync("/api/v1/partners/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CreateInviteResponse>();

        await _partnerClient.PostAsJsonAsync("/api/v1/partners/accept",
            new AcceptInviteRequest(invite!.InviteCode));

        var response = await _client.PostAsync($"/api/v1/partners/{invite.PartnershipId}/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PartnershipResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Ended");
    }

    [Fact]
    public async Task ListPartnerships_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/partners");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PartnershipListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }
}
