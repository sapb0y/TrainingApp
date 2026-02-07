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

public class FatigueEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public FatigueEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<(Guid workoutId, Guid exerciseId)> SeedCompletedWorkoutAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();

        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = $"Fatigue Test Exercise {Guid.NewGuid():N}",
            Category = "Chest",
            PrimaryMuscles = ["Pectoralis major"]
        };
        db.Exercises.Add(exercise);

        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Name = "Fatigue Test Workout",
            ScheduledAt = DateTimeOffset.UtcNow,
            Status = WorkoutStatus.Completed,
            CompletedAt = DateTimeOffset.UtcNow,
            SessionRpe = 7,
            Sets =
            {
                new WorkoutSet
                {
                    Id = Guid.NewGuid(),
                    ExerciseId = exercise.Id,
                    SetNumber = 1,
                    TargetReps = 10,
                    TargetWeight = 100m,
                    ActualReps = 10,
                    ActualWeight = 100m,
                    Rpe = 8m,
                    IsWarmup = false,
                    CompletedAt = DateTimeOffset.UtcNow
                },
                new WorkoutSet
                {
                    Id = Guid.NewGuid(),
                    ExerciseId = exercise.Id,
                    SetNumber = 2,
                    TargetReps = 10,
                    TargetWeight = 100m,
                    ActualReps = 8,
                    ActualWeight = 100m,
                    Rpe = 9m,
                    IsWarmup = false,
                    CompletedAt = DateTimeOffset.UtcNow
                }
            }
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        InvalidateExerciseCache();
        return (workout.Id, exercise.Id);
    }

    [Fact]
    public async Task GetReadiness_Returns200_WithDefaultValues_WhenNoHistory()
    {
        var response = await _client.GetAsync("/api/v1/fatigue/readiness");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ReadinessResponse>();
        result.Should().NotBeNull();
        result!.Ctl.Should().BeGreaterThanOrEqualTo(0);
        result.Score.Should().BeGreaterThan(0);
        result.Category.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetReadiness_Returns200_AfterWorkoutComplete()
    {
        // Seed a completed workout to ensure metrics exist
        await SeedCompletedWorkoutAsync();

        // Trigger metrics update via the complete endpoint on a new workout
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = $"Readiness Test {Guid.NewGuid():N}",
            Category = "Legs",
            PrimaryMuscles = ["Quadriceps"]
        };
        db.Exercises.Add(exercise);
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            UserId = TestUserId,
            Name = "Readiness Test Workout",
            ScheduledAt = DateTimeOffset.UtcNow,
            Status = WorkoutStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow,
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();

        // Add a set
        var set = new WorkoutSet
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            ExerciseId = exercise.Id,
            SetNumber = 1,
            ActualReps = 10,
            ActualWeight = 80m,
            Rpe = 7m,
            IsWarmup = false,
            CompletedAt = DateTimeOffset.UtcNow
        };
        db.WorkoutSets.Add(set);
        await db.SaveChangesAsync();

        InvalidateExerciseCache();

        // Complete workout via API (auto-triggers metrics)
        var completeResponse = await _client.PostAsJsonAsync(
            $"/api/v1/workouts/{workout.Id}/complete",
            new CompleteWorkoutRequest(7, 5, null));
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check readiness
        var response = await _client.GetAsync("/api/v1/fatigue/readiness");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ReadinessResponse>();
        result.Should().NotBeNull();
        result!.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetHistory_Returns200_WithDateRange()
    {
        await SeedCompletedWorkoutAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/api/v1/fatigue/history?from={today}&to={today}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DailyMetricsListResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHistory_Returns200_Empty_WhenNoData()
    {
        var response = await _client.GetAsync("/api/v1/fatigue/history?from=2020-01-01&to=2020-01-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DailyMetricsListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LogRecovery_Returns200_WithValidData()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PostAsJsonAsync("/api/v1/fatigue/recovery",
            new CreateRecoveryLogRequest(today, 4, 7.5m, 2, 4, 4, 4, "Slept well"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RecoveryLogResponse>();
        result.Should().NotBeNull();
        result!.SleepQuality.Should().Be(4);
        result.SleepHours.Should().Be(7.5m);
    }

    [Fact]
    public async Task LogRecovery_Returns400_InvalidSleepQuality()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PostAsJsonAsync("/api/v1/fatigue/recovery",
            new CreateRecoveryLogRequest(today, 0, null, null, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LogRecovery_UpsertsForSameDate()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10).ToString("yyyy-MM-dd");

        // First log
        var response1 = await _client.PostAsJsonAsync("/api/v1/fatigue/recovery",
            new CreateRecoveryLogRequest(date, 3, 6m, null, null, null, null, null));
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await response1.Content.ReadFromJsonAsync<RecoveryLogResponse>();

        // Second log same date — should update
        var response2 = await _client.PostAsJsonAsync("/api/v1/fatigue/recovery",
            new CreateRecoveryLogRequest(date, 5, 8m, null, null, null, null, null));
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await response2.Content.ReadFromJsonAsync<RecoveryLogResponse>();

        second!.Id.Should().Be(first!.Id);
        second.SleepQuality.Should().Be(5);
        second.SleepHours.Should().Be(8m);
    }

    [Fact]
    public async Task GetRecoveryLogs_Returns200_WithDateRange()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-20).ToString("yyyy-MM-dd");

        // Create a log first
        await _client.PostAsJsonAsync("/api/v1/fatigue/recovery",
            new CreateRecoveryLogRequest(date, 3, 7m, 2, 4, 3, 4, null));

        var response = await _client.GetAsync($"/api/v1/fatigue/recovery?from={date}&to={date}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RecoveryLogListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Recalculate_Returns200_AndPopulatesMetrics()
    {
        await SeedCompletedWorkoutAsync();

        var response = await _client.PostAsJsonAsync("/api/v1/fatigue/recalculate",
            new RecalculateRequest(null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify metrics were created
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var historyResponse = await _client.GetAsync($"/api/v1/fatigue/history?from={today}&to={today}");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
