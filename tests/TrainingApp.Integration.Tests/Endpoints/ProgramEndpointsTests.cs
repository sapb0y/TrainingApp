using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests.Endpoints;

public class ProgramEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProgramEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task SeedExercisesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();

        if (await db.Exercises.AnyAsync())
            return;

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
    }

    private async Task<ProgramResponse> GenerateTestProgramAsync(string template = "PushPullLegs", int weeks = 12)
    {
        await SeedExercisesAsync();

        var request = new GenerateProgramRequest(
            $"Test {template} Program",
            "Hypertrophy",
            template,
            weeks,
            "2025-06-01");

        var response = await _client.PostAsJsonAsync("/api/v1/programs/generate", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ProgramResponse>();
        result.Should().NotBeNull();
        return result!;
    }

    [Fact]
    public async Task GenerateProgram_ReturnsCreated_WithFullHierarchy()
    {
        var program = await GenerateTestProgramAsync();

        program.Name.Should().Be("Test PushPullLegs Program");
        program.Goal.Should().Be("Hypertrophy");
        program.Template.Should().Be("PushPullLegs");
        program.Status.Should().Be("Draft");
        program.DurationWeeks.Should().Be(12);
        program.Phases.Should().NotBeEmpty();

        // Each phase should have workouts and volume targets
        foreach (var phase in program.Phases)
        {
            phase.VolumeTargets.Should().NotBeEmpty();
            phase.Workouts.Should().NotBeEmpty();

            foreach (var workout in phase.Workouts)
            {
                workout.Exercises.Should().NotBeEmpty();
            }
        }
    }

    [Fact]
    public async Task GetPrograms_ReturnsEmptyList_WhenNone()
    {
        // Clean programs for this test
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            db.Programs.RemoveRange(db.Programs);
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/v1/programs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProgramListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProgramById_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync($"/api/v1/programs/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProgramById_ReturnsFullHierarchy()
    {
        var created = await GenerateTestProgramAsync();

        var response = await _client.GetAsync($"/api/v1/programs/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var program = await response.Content.ReadFromJsonAsync<ProgramResponse>();
        program.Should().NotBeNull();
        program!.Id.Should().Be(created.Id);
        program.Phases.Should().NotBeEmpty();
        program.Phases.First().Workouts.Should().NotBeEmpty();
        program.Phases.First().Workouts.First().Exercises.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateProgram_UpdatesName()
    {
        var created = await GenerateTestProgramAsync();

        var updateRequest = new UpdateProgramRequest("Updated Name", "New description", "Active");
        var response = await _client.PutAsJsonAsync($"/api/v1/programs/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProgramResponse>();
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("New description");
        updated.Status.Should().Be("Active");
    }

    [Fact]
    public async Task DeleteProgram_RemovesProgram()
    {
        var created = await GenerateTestProgramAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/programs/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/programs/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateProgram_ReturnsBadRequest_WhenInvalidTemplate()
    {
        var request = new GenerateProgramRequest("Test", "Hypertrophy", "InvalidTemplate", 12, "2025-06-01");
        var response = await _client.PostAsJsonAsync("/api/v1/programs/generate", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateProgram_ReturnsBadRequest_WhenNoExercises()
    {
        // Clear all exercises
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            // Need to clear program exercises first due to FK
            db.ProgramExercises.RemoveRange(db.ProgramExercises);
            await db.SaveChangesAsync();
            db.Exercises.RemoveRange(db.Exercises);
            await db.SaveChangesAsync();
        }

        var request = new GenerateProgramRequest("Test", "Hypertrophy", "PushPullLegs", 12, "2025-06-01");
        var response = await _client.PostAsJsonAsync("/api/v1/programs/generate", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
