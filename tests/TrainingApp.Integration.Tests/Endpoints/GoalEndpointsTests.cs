using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class GoalEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GoalEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGoal_Returns200()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/goals",
            new CreateGoalRequest("Bench Press 100kg", "Strength", 100m, "kg", null, 80m, "2025-12-31", "Main goal"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GoalResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Bench Press 100kg");
        result.Type.Should().Be("Strength");
        result.Status.Should().Be("Active");
        result.TargetValue.Should().Be(100m);
    }

    [Fact]
    public async Task GetGoals_Returns200_ListsActiveGoals()
    {
        await _client.PostAsJsonAsync("/api/v1/goals",
            new CreateGoalRequest("Goal for listing", "Custom", null, null, null, null, null, null));

        var response = await _client.GetAsync("/api/v1/goals?status=Active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GoalListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetGoalDetail_Returns200_WithCheckpoints()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/goals",
            new CreateGoalRequest("Detail goal", "BodyWeight", 75m, "kg", null, 85m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalResponse>();

        var response = await _client.GetAsync($"/api/v1/goals/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GoalDetailResponse>();
        result.Should().NotBeNull();
        result!.Goal.Id.Should().Be(created.Id);
        result.Checkpoints.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateGoal_Returns200_MarkAchieved()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/goals",
            new CreateGoalRequest("Achievable goal", "Custom", 100m, "kg", null, 50m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalResponse>();

        var response = await _client.PutAsJsonAsync($"/api/v1/goals/{created!.Id}",
            new UpdateGoalRequest("Achieved", null, null, "Done!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GoalResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Achieved");
        result.AchievedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteGoal_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/goals",
            new CreateGoalRequest("Deletable goal", "Custom", null, null, null, null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalResponse>();

        var response = await _client.DeleteAsync($"/api/v1/goals/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/goals/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddCheckpoint_Returns200_CalculatesPercent()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/goals",
            new CreateGoalRequest("Checkpoint goal", "Strength", 100m, "kg", null, 80m, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalResponse>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PostAsJsonAsync($"/api/v1/goals/{created!.Id}/checkpoints",
            new AddCheckpointRequest(today, 90m, "Halfway there"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GoalCheckpointResponse>();
        result.Should().NotBeNull();
        result!.Value.Should().Be(90m);
        result.PercentComplete.Should().Be(50m); // (90-80)/(100-80) * 100 = 50%
    }

    [Fact]
    public async Task GetGoals_FiltersByStatus()
    {
        var response = await _client.GetAsync("/api/v1/goals?status=Achieved");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GoalListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().OnlyContain(g => g.Status == "Achieved");
    }

    [Fact]
    public async Task CreateGoal_WithExerciseId_Returns200()
    {
        var exerciseId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/api/v1/goals",
            new CreateGoalRequest("Squat 150kg", "Strength", 150m, "kg", exerciseId, 120m, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GoalResponse>();
        result.Should().NotBeNull();
        result!.ExerciseId.Should().Be(exerciseId);
    }
}
