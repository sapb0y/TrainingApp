using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests.Endpoints;

public class ExerciseEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ExerciseEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetExercises_ReturnsEmptyList_WhenNoExercises()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/exercises");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExerciseListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetExerciseById_ReturnsNotFound_WhenExerciseDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/exercises/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetExerciseById_ReturnsExercise_WhenExists()
    {
        // Arrange
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Bench Press",
            Category = "Chest",
            PrimaryMuscles = ["Pectoralis Major"],
            SecondaryMuscles = ["Triceps"],
            Equipment = ["Barbell"]
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/exercises/{exercise.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExerciseResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Bench Press");
        result.Category.Should().Be("Chest");
    }

    [Fact]
    public async Task SearchExercises_ReturnsMatchingExercises()
    {
        // Arrange
        var exercise = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = "Barbell Squat",
            Category = "Legs"
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            db.Exercises.Add(exercise);
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/v1/exercises/search?q=squat");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExerciseListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(e => e.Name == "Barbell Squat");
    }

    [Fact]
    public async Task SearchExercises_ReturnsBadRequest_WhenQueryTooShort()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/exercises/search?q=a");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
