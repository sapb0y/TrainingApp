using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Api.Contracts;
using TrainingApp.Core.Entities;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests.Endpoints;

public class DeficitEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public DeficitEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task ClearActiveDeficits()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
        var active = await db.DeficitPhases
            .Where(d => d.UserId == TestUserId && d.Status == DeficitPhaseStatus.Active)
            .ToListAsync();
        foreach (var d in active)
        {
            d.Status = DeficitPhaseStatus.Completed;
            d.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        var paused = await db.DeficitPhases
            .Where(d => d.UserId == TestUserId && d.Status == DeficitPhaseStatus.Paused)
            .ToListAsync();
        foreach (var d in paused)
        {
            d.Status = DeficitPhaseStatus.Completed;
            d.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task StartDeficit_Returns200_WithValidData()
    {
        await ClearActiveDeficits();

        var response = await _client.PostAsJsonAsync("/api/v1/deficit/start",
            new StartDeficitRequest(85.0m, 75.0m, 0.5m, "Moderate", 6, "Starting cut"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeficitPhaseResponse>();
        result.Should().NotBeNull();
        result!.StartWeightKg.Should().Be(85.0m);
        result.Strategy.Should().Be("Moderate");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task StartDeficit_Returns409_WhenAlreadyActive()
    {
        await ClearActiveDeficits();

        // Start first
        await _client.PostAsJsonAsync("/api/v1/deficit/start",
            new StartDeficitRequest(85.0m, null, 0.5m, "Moderate", null, null));

        // Try to start second
        var response = await _client.PostAsJsonAsync("/api/v1/deficit/start",
            new StartDeficitRequest(84.0m, null, 0.5m, "Conservative", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetActiveDeficit_Returns200_WhenExists()
    {
        await ClearActiveDeficits();

        await _client.PostAsJsonAsync("/api/v1/deficit/start",
            new StartDeficitRequest(85.0m, null, 0.5m, "Moderate", null, null));

        var response = await _client.GetAsync("/api/v1/deficit/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeficitPhaseResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Active");
    }

    [Fact]
    public async Task EndDeficit_Returns200_WhenActive()
    {
        await ClearActiveDeficits();

        await _client.PostAsJsonAsync("/api/v1/deficit/start",
            new StartDeficitRequest(85.0m, null, 0.5m, "Moderate", null, null));

        var response = await _client.PostAsync("/api/v1/deficit/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeficitPhaseResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
        result.EndDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDeficitHistory_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/deficit/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DeficitPhaseListResponse>();
        result.Should().NotBeNull();
    }
}
