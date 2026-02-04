using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests.Endpoints;

public class WorkoutEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly Guid TempUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public WorkoutEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWorkouts_ReturnsEmptyList_WhenNoWorkouts()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/workouts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateWorkout_ReturnsCreatedWorkout()
    {
        // Arrange
        var request = new CreateWorkoutRequest(
            "Monday Push",
            DateTimeOffset.UtcNow.AddDays(1),
            "Chest and triceps");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/workouts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WorkoutResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Monday Push");
        result.Notes.Should().Be("Chest and triceps");
        result.Status.Should().Be("Planned");
    }

    [Fact]
    public async Task GetWorkoutById_ReturnsNotFound_WhenWorkoutDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/workouts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateWorkout_UpdatesExistingWorkout()
    {
        // Arrange - create workout first
        var createRequest = new CreateWorkoutRequest("Test Workout", DateTimeOffset.UtcNow, null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workouts", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<WorkoutResponse>();

        var updateRequest = new UpdateWorkoutRequest(
            "Updated Workout",
            null,
            "Updated notes",
            WorkoutStatus.InProgress,
            7);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/workouts/{created!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutResponse>();
        result!.Name.Should().Be("Updated Workout");
        result.Notes.Should().Be("Updated notes");
        result.Status.Should().Be("InProgress");
        result.SessionRpe.Should().Be(7);
    }

    [Fact]
    public async Task DeleteWorkout_RemovesWorkout()
    {
        // Arrange
        var createRequest = new CreateWorkoutRequest("To Delete", DateTimeOffset.UtcNow, null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workouts", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<WorkoutResponse>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/v1/workouts/{created!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/workouts/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateSet_AddsSetToWorkout()
    {
        // Arrange - create workout and exercise
        var createRequest = new CreateWorkoutRequest("Workout with Sets", DateTimeOffset.UtcNow, null);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/workouts", createRequest);
        var workout = await createResponse.Content.ReadFromJsonAsync<WorkoutResponse>();

        Guid exerciseId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            var exercise = new Exercise
            {
                Id = Guid.NewGuid(),
                Name = "Test Exercise",
                Category = "Test"
            };
            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();
            exerciseId = exercise.Id;
        }

        var setRequest = new CreateWorkoutSetRequest(exerciseId, 1, 10, 100m, false);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/workouts/{workout!.Id}/sets", setRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSetResponse>();
        result.Should().NotBeNull();
        result!.SetNumber.Should().Be(1);
        result.TargetReps.Should().Be(10);
        result.TargetWeight.Should().Be(100m);
    }
}
