using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests.Endpoints;

public class AutoregulationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AutoregulationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void InvalidateExerciseCache()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        cache.Remove("exercises_all");
    }

    private async Task<(Guid workoutId, Guid exerciseId)> SeedWorkoutWithExerciseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Autoreg Bench Press",
            Category = "Chest",
            PrimaryMuscles = ["Pectoralis major"]
        };
        db.Exercises.Add(exercise);

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Name = "Autoreg Test Workout",
            ScheduledAt = DateTimeOffset.UtcNow,
            Status = WorkoutStatus.Planned
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        InvalidateExerciseCache();
        return (workout.Id, exercise.Id);
    }

    [Fact]
    public async Task StartWorkout_SetsStatusToInProgress()
    {
        var (workoutId, _) = await SeedWorkoutWithExerciseAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/start",
            new StartWorkoutRequest(7));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("InProgress");
        result.PreWorkoutReadiness.Should().Be(7);
        result.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task StartWorkout_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{Guid.NewGuid()}/start",
            new StartWorkoutRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteWorkout_SetsCompletionFields()
    {
        var (workoutId, _) = await SeedWorkoutWithExerciseAsync();

        // Start first
        await _client.PostAsJsonAsync($"/api/v1/workouts/{workoutId}/start", new StartWorkoutRequest(null));

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/complete",
            new CompleteWorkoutRequest(8, 6, "Felt good"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
        result.SessionRpe.Should().Be(8);
        result.PostWorkoutFatigue.Should().Be(6);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecommendations_ReturnsAdjustment_WhenRirDriftHigh()
    {
        var (workoutId, exerciseId) = await SeedWorkoutWithExerciseAsync();

        // Start workout
        await _client.PostAsJsonAsync($"/api/v1/workouts/{workoutId}/start", new StartWorkoutRequest(null));

        // Create a set with high drift (target 3, actual 1 = drift -2)
        var createSetResponse = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/sets",
            new CreateWorkoutSetRequest(exerciseId, 1, 8, 100m, 3, false));
        createSetResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSet = await createSetResponse.Content.ReadFromJsonAsync<WorkoutSetResponse>();

        // Complete set with RIR=1 (drift = 1-3 = -2)
        await _client.PutAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/sets/{createdSet!.Id}",
            new UpdateWorkoutSetRequest(null, null, 8, 100m, null, 1, null, null, DateTimeOffset.UtcNow, null));

        // Get recommendations
        var response = await _client.GetAsync($"/api/v1/workouts/{workoutId}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutRecommendationsResponse>();
        result.Should().NotBeNull();
        result!.Exercises.Should().NotBeEmpty();
        var exerciseRec = result.Exercises.First();
        exerciseRec.NextSet.Should().NotBeNull();
        exerciseRec.NextSet!.Weight.Should().BeLessThan(100m); // reduced weight
    }

    [Fact]
    public async Task GetRecommendations_ReturnsMaintain_WhenOnTarget()
    {
        var (workoutId, exerciseId) = await SeedWorkoutWithExerciseAsync();

        await _client.PostAsJsonAsync($"/api/v1/workouts/{workoutId}/start", new StartWorkoutRequest(null));

        // Set with drift = 0 (target=2, actual=2)
        var createSetResponse = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/sets",
            new CreateWorkoutSetRequest(exerciseId, 1, 8, 80m, 2, false));
        var createdSet = await createSetResponse.Content.ReadFromJsonAsync<WorkoutSetResponse>();

        await _client.PutAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/sets/{createdSet!.Id}",
            new UpdateWorkoutSetRequest(null, null, 8, 80m, null, 2, null, null, DateTimeOffset.UtcNow, null));

        var response = await _client.GetAsync($"/api/v1/workouts/{workoutId}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutRecommendationsResponse>();
        result.Should().NotBeNull();
        result!.Exercises.Should().NotBeEmpty();
        var exerciseRec = result.Exercises.First();
        exerciseRec.NextSet.Should().NotBeNull();
        exerciseRec.NextSet!.Weight.Should().Be(80m); // maintain
    }

    [Fact]
    public async Task GetRecommendations_ReturnsVolumeSkip_WhenSeverelyFatigued()
    {
        var (workoutId, exerciseId) = await SeedWorkoutWithExerciseAsync();

        await _client.PostAsJsonAsync($"/api/v1/workouts/{workoutId}/start", new StartWorkoutRequest(null));

        // Create 3 sets with severe negative drift
        for (int i = 1; i <= 3; i++)
        {
            var createResp = await _client.PostAsJsonAsync(
                $"/api/v1/workouts/{workoutId}/sets",
                new CreateWorkoutSetRequest(exerciseId, i, 8, 100m, 3, false));
            var set = await createResp.Content.ReadFromJsonAsync<WorkoutSetResponse>();

            await _client.PutAsJsonAsync(
                $"/api/v1/workouts/{workoutId}/sets/{set!.Id}",
                new UpdateWorkoutSetRequest(null, null, 8, 100m, null, 0, null, null, DateTimeOffset.UtcNow, null));
        }

        var response = await _client.GetAsync($"/api/v1/workouts/{workoutId}/recommendations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkoutRecommendationsResponse>();
        result.Should().NotBeNull();
        result!.Exercises.Should().NotBeEmpty();
        result.Exercises.First().VolumeAction.Should().Be("SkipRemaining");
    }

    [Fact]
    public async Task GetAdaptationLog_ReturnsLogEntries()
    {
        var (workoutId, exerciseId) = await SeedWorkoutWithExerciseAsync();

        await _client.PostAsJsonAsync($"/api/v1/workouts/{workoutId}/start", new StartWorkoutRequest(null));

        // Create set with drift that triggers adjustment
        var createResp = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/sets",
            new CreateWorkoutSetRequest(exerciseId, 1, 8, 100m, 3, false));
        var set = await createResp.Content.ReadFromJsonAsync<WorkoutSetResponse>();

        await _client.PutAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/sets/{set!.Id}",
            new UpdateWorkoutSetRequest(null, null, 8, 100m, null, 0, null, null, DateTimeOffset.UtcNow, null));

        // Trigger recommendations (creates log entries)
        await _client.GetAsync($"/api/v1/workouts/{workoutId}/recommendations");

        // Get adaptation log
        var response = await _client.GetAsync($"/api/v1/workouts/{workoutId}/adaptation-log");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<List<AdaptationLogResponse>>();
        logs.Should().NotBeNull();
        logs!.Should().NotBeEmpty();
        logs.First().RuleName.Should().Be("LoadReduction");
    }

    [Fact]
    public async Task CreateSet_WithTargetRir_IncludedInResponse()
    {
        var (workoutId, exerciseId) = await SeedWorkoutWithExerciseAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{workoutId}/sets",
            new CreateWorkoutSetRequest(exerciseId, 1, 10, 80m, 2, false));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<WorkoutSetResponse>();
        result.Should().NotBeNull();
        result!.TargetRir.Should().Be(2);
        result.RirDrift.Should().BeNull(); // no actual RIR yet
        result.WasAutoAdjusted.Should().BeFalse();
    }
}
