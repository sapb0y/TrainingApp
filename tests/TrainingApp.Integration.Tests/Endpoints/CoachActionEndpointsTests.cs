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

public class CoachActionEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _athleteClient;
    private readonly HttpClient _coachClient;
    private readonly CustomWebApplicationFactory _factory;

    public CoachActionEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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

    private Guid GetAthleteId() => Guid.Parse("00000000-0000-0000-0000-000000000001");

    private void InvalidateExerciseCache()
    {
        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
        cache.Remove("exercises_all");
    }

    private async Task SeedExercisesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();

        if (await db.Exercises.AnyAsync())
        {
            InvalidateExerciseCache();
            return;
        }

        var exercises = new[]
        {
            new Exercise { Id = Guid.NewGuid(), Name = "Bench Press", Category = "Chest", PrimaryMuscles = ["Pectoralis major"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Incline Dumbbell Press", Category = "Chest", PrimaryMuscles = ["Pectoralis major"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Barbell Row", Category = "Back", PrimaryMuscles = ["Latissimus dorsi"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Pull Up", Category = "Back", PrimaryMuscles = ["Latissimus dorsi"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Overhead Press", Category = "Shoulders", PrimaryMuscles = ["Deltoid"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Lateral Raise", Category = "Shoulders", PrimaryMuscles = ["Deltoid"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Squat", Category = "Legs", PrimaryMuscles = ["Quadriceps"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Leg Press", Category = "Legs", PrimaryMuscles = ["Quadriceps"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Romanian Deadlift", Category = "Legs", PrimaryMuscles = ["Hamstrings"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Hip Thrust", Category = "Legs", PrimaryMuscles = ["Gluteus maximus"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Barbell Curl", Category = "Arms", PrimaryMuscles = ["Biceps brachii"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Tricep Pushdown", Category = "Arms", PrimaryMuscles = ["Triceps brachii"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Calf Raise", Category = "Legs", PrimaryMuscles = ["Gastrocnemius"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Crunch", Category = "Abs", PrimaryMuscles = ["Rectus abdominis"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Shrug", Category = "Shoulders", PrimaryMuscles = ["Trapezius"] },
            new Exercise { Id = Guid.NewGuid(), Name = "Wrist Curl", Category = "Arms", PrimaryMuscles = ["Brachioradialis"] },
        };

        db.Exercises.AddRange(exercises);
        await db.SaveChangesAsync();
        InvalidateExerciseCache();
    }

    [Fact]
    public async Task GenerateAthleteProgram_CreatesProgram()
    {
        await SetupCoachAthleteLink();
        await SeedExercisesAsync();

        var req = new GenerateProgramRequest("Coach Test Program", "Hypertrophy", "UpperLower", 8, "2026-03-01");
        var response = await _coachClient.PostAsJsonAsync($"/api/v1/coach/athletes/{GetAthleteId()}/programs/generate", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProgramSummaryResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Coach Test Program");
    }

    [Fact]
    public async Task ListAthletePrograms_ReturnsPrograms()
    {
        await SetupCoachAthleteLink();

        var response = await _coachClient.GetAsync($"/api/v1/coach/athletes/{GetAthleteId()}/programs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProgramListResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ModifyAthleteSet_PlannedWorkout_Returns200()
    {
        await SetupCoachAthleteLink();
        await SeedExercisesAsync();

        // Create a workout with a planned set for the athlete
        var workoutReq = new CreateWorkoutRequest("Coach Test Workout", DateTimeOffset.UtcNow.AddDays(1), null);
        var workoutResponse = await _athleteClient.PostAsJsonAsync("/api/v1/workouts", workoutReq);
        workoutResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var workout = await workoutResponse.Content.ReadFromJsonAsync<WorkoutResponse>();

        // Get an exercise ID
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var exercise = await db.Exercises.FirstAsync();

        // Add a set
        var setReq = new { exerciseId = exercise.Id, setNumber = 1, targetReps = 10, targetWeight = 60.0, isWarmup = false };
        var setResponse = await _athleteClient.PostAsJsonAsync($"/api/v1/workouts/{workout!.Id}/sets", setReq);
        setResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var setResult = await setResponse.Content.ReadFromJsonAsync<WorkoutSetResponse>();

        // Coach modifies the set
        var modifyReq = new CoachModifySetRequest(12, 65m, null);
        var response = await _coachClient.PutAsJsonAsync(
            $"/api/v1/coach/athletes/{GetAthleteId()}/workouts/{workout.Id}/sets/{setResult!.Id}", modifyReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ModifyAthleteSet_CompletedWorkout_ReturnsConflict()
    {
        await SetupCoachAthleteLink();
        await SeedExercisesAsync();

        // Create + complete a workout
        var workoutReq = new CreateWorkoutRequest("Completed Workout", DateTimeOffset.UtcNow.AddDays(-1), null);
        var workoutResponse = await _athleteClient.PostAsJsonAsync("/api/v1/workouts", workoutReq);
        var workout = await workoutResponse.Content.ReadFromJsonAsync<WorkoutResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var exercise = await db.Exercises.FirstAsync();

        var setReq = new { exerciseId = exercise.Id, setNumber = 1, targetReps = 10, targetWeight = 60.0, isWarmup = false };
        var setResponse = await _athleteClient.PostAsJsonAsync($"/api/v1/workouts/{workout!.Id}/sets", setReq);
        var setResult = await setResponse.Content.ReadFromJsonAsync<WorkoutSetResponse>();

        // Complete the workout
        await _athleteClient.PutAsJsonAsync($"/api/v1/workouts/{workout.Id}",
            new UpdateWorkoutRequest(null, null, null, WorkoutStatus.Completed, null));

        var modifyReq = new CoachModifySetRequest(12, null, null);
        var response = await _coachClient.PutAsJsonAsync(
            $"/api/v1/coach/athletes/{GetAthleteId()}/workouts/{workout.Id}/sets/{setResult!.Id}", modifyReq);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddNote_CreatesNote()
    {
        await SetupCoachAthleteLink();

        var req = new CoachNoteRequest("Great progress on squats!", null, null);
        var response = await _coachClient.PostAsJsonAsync($"/api/v1/coach/athletes/{GetAthleteId()}/notes", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CoachNoteResponse>();
        result.Should().NotBeNull();
        result!.Content.Should().Be("Great progress on squats!");
    }

    [Fact]
    public async Task ListNotes_ReturnsNotes()
    {
        await SetupCoachAthleteLink();

        await _coachClient.PostAsJsonAsync($"/api/v1/coach/athletes/{GetAthleteId()}/notes",
            new CoachNoteRequest("Test note", null, null));

        var response = await _coachClient.GetAsync($"/api/v1/coach/athletes/{GetAthleteId()}/notes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoachNoteListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddNote_WithWorkoutContext_CreatesNote()
    {
        await SetupCoachAthleteLink();

        var workoutReq = new CreateWorkoutRequest("Note Context Workout", DateTimeOffset.UtcNow, null);
        var workoutResponse = await _athleteClient.PostAsJsonAsync("/api/v1/workouts", workoutReq);
        var workout = await workoutResponse.Content.ReadFromJsonAsync<WorkoutResponse>();

        var req = new CoachNoteRequest("Watch form on this workout", workout!.Id, null);
        var response = await _coachClient.PostAsJsonAsync($"/api/v1/coach/athletes/{GetAthleteId()}/notes", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CoachNoteResponse>();
        result!.WorkoutId.Should().Be(workout.Id);
    }

    [Fact]
    public async Task Actions_WithoutPermission_Returns403()
    {
        await SetupCoachAthleteLink();

        // Revoke CanModifyProgram
        await _coachClient.PutAsJsonAsync($"/api/v1/coach/athletes/{GetAthleteId()}/permissions",
            new UpdatePermissionsRequest(false, null, null));

        await SeedExercisesAsync();
        var req = new GenerateProgramRequest("Blocked Program", "Hypertrophy", "FullBody", 4, "2026-03-01");
        var response = await _coachClient.PostAsJsonAsync($"/api/v1/coach/athletes/{GetAthleteId()}/programs/generate", req);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
