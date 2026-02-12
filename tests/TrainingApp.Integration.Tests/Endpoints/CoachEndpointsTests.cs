using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class CoachEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _athleteClient;
    private readonly HttpClient _coachClient;

    public CoachEndpointsTests(CustomWebApplicationFactory factory)
    {
        _athleteClient = factory.CreateClient(); // default test user (athlete)
        _coachClient = factory.CreateCoachClient();
    }

    /// <summary>End any existing active coach relationship so tests are isolated.</summary>
    private async Task CleanupActiveCoachRelationships()
    {
        var listResp = await _coachClient.GetAsync("/api/v1/coach/athletes");
        if (!listResp.IsSuccessStatusCode) return;
        var list = await listResp.Content.ReadFromJsonAsync<CoachAthleteListResponse>();
        if (list?.Items is null) return;

        foreach (var item in list.Items.Where(i => i.Status == "Active"))
        {
            await _coachClient.PostAsync($"/api/v1/coach/athletes/{item.AthleteId}/end", null);
        }
    }

    [Fact]
    public async Task CreateInvite_AsCoach_Returns200WithCode()
    {
        var response = await _coachClient.PostAsync("/api/v1/coach/invite", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachInviteResponse>();
        result.Should().NotBeNull();
        result!.InviteCode.Should().HaveLength(6);
        result.RelationshipId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AcceptInvite_ValidCode_ActivatesRelationship()
    {
        await CleanupActiveCoachRelationships();

        var inviteResponse = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CoachInviteResponse>();

        var response = await _athleteClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(invite!.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachAthleteResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Active");
        result.AthleteId.Should().NotBeNull();
    }

    [Fact]
    public async Task ListAthletes_AfterAccept_ReturnsRoster()
    {
        await CleanupActiveCoachRelationships();

        var inviteResponse = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CoachInviteResponse>();
        await _athleteClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(invite!.InviteCode));

        var response = await _coachClient.GetAsync("/api/v1/coach/athletes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachAthleteListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAthlete_LinkedAthlete_ReturnsDetail()
    {
        await CleanupActiveCoachRelationships();

        var inviteResponse = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CoachInviteResponse>();
        var acceptResponse = await _athleteClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(invite!.InviteCode));
        var accepted = await acceptResponse.Content.ReadFromJsonAsync<CoachAthleteResponse>();

        var response = await _coachClient.GetAsync($"/api/v1/coach/athletes/{accepted!.AthleteId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachAthleteResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Active");
    }

    [Fact]
    public async Task EndRelationship_ActiveRelationship_ReturnsEnded()
    {
        await CleanupActiveCoachRelationships();

        var inviteResponse = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CoachInviteResponse>();
        var acceptResponse = await _athleteClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(invite!.InviteCode));
        var accepted = await acceptResponse.Content.ReadFromJsonAsync<CoachAthleteResponse>();

        var response = await _coachClient.PostAsync($"/api/v1/coach/athletes/{accepted!.AthleteId}/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachAthleteResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Ended");
    }

    [Fact]
    public async Task AcceptInvite_SelfInvite_ReturnsConflict()
    {
        var inviteResponse = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CoachInviteResponse>();

        // Coach tries to accept own invite
        var response = await _coachClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(invite!.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AcceptInvite_WhenAthleteHasCoach_ReturnsConflict()
    {
        await CleanupActiveCoachRelationships();

        // First coach-athlete link
        var invite1 = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var code1 = await invite1.Content.ReadFromJsonAsync<CoachInviteResponse>();
        await _athleteClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(code1!.InviteCode));

        // Second invite from same coach
        var invite2 = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var code2 = await invite2.Content.ReadFromJsonAsync<CoachInviteResponse>();

        // Athlete already has active coach — should fail
        var response = await _athleteClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(code2!.InviteCode));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
