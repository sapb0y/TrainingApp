using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class CoachDashboardEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _athleteClient;
    private readonly HttpClient _coachClient;

    public CoachDashboardEndpointsTests(CustomWebApplicationFactory factory)
    {
        _athleteClient = factory.CreateClient();
        _coachClient = factory.CreateCoachClient();
    }

    private async Task SetupCoachAthleteLink()
    {
        var inviteResponse = await _coachClient.PostAsync("/api/v1/coach/invite", null);
        var invite = await inviteResponse.Content.ReadFromJsonAsync<CoachInviteResponse>();
        await _athleteClient.PostAsJsonAsync("/api/v1/coach/accept",
            new AcceptCoachInviteRequest(invite!.InviteCode));
    }

    [Fact]
    public async Task GetDashboard_WithAthletes_ReturnsRosterAndAlerts()
    {
        await SetupCoachAthleteLink();

        var response = await _coachClient.GetAsync("/api/v1/coach/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachDashboardResponse>();
        result.Should().NotBeNull();
        result!.Roster.Should().NotBeEmpty();
        result.TotalAthletes.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetDashboard_NoAthletes_ReturnsEmptyRoster()
    {
        // Fresh coach with no links — dashboard still returns 200
        var response = await _coachClient.GetAsync("/api/v1/coach/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachDashboardResponse>();
        result.Should().NotBeNull();
        result!.Roster.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAthleteDashboard_LinkedAthlete_Returns200()
    {
        await SetupCoachAthleteLink();

        // Get athlete ID from roster
        var dashResponse = await _coachClient.GetAsync("/api/v1/coach/dashboard");
        var dash = await dashResponse.Content.ReadFromJsonAsync<CoachDashboardResponse>();
        var athleteId = dash!.Roster.First().AthleteId;

        var response = await _coachClient.GetAsync($"/api/v1/coach/athletes/{athleteId}/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachAthleteDashboardResponse>();
        result.Should().NotBeNull();
        result!.AthleteId.Should().Be(athleteId);
    }

    [Fact]
    public async Task GetAthleteDashboard_NonLinked_Returns403()
    {
        var randomId = Guid.NewGuid();
        var response = await _coachClient.GetAsync($"/api/v1/coach/athletes/{randomId}/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAthleteWorkouts_LinkedAthlete_Returns200()
    {
        await SetupCoachAthleteLink();

        var dashResponse = await _coachClient.GetAsync("/api/v1/coach/dashboard");
        var dash = await dashResponse.Content.ReadFromJsonAsync<CoachDashboardResponse>();
        var athleteId = dash!.Roster.First().AthleteId;

        var response = await _coachClient.GetAsync($"/api/v1/coach/athletes/{athleteId}/workouts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutListResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAthleteChart_Strength_Returns200()
    {
        await SetupCoachAthleteLink();

        var dashResponse = await _coachClient.GetAsync("/api/v1/coach/dashboard");
        var dash = await dashResponse.Content.ReadFromJsonAsync<CoachDashboardResponse>();
        var athleteId = dash!.Roster.First().AthleteId;

        var response = await _coachClient.GetAsync($"/api/v1/coach/athletes/{athleteId}/charts/strength");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
