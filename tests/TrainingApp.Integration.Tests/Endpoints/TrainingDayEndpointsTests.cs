using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class TrainingDayEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TrainingDayEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_Returns200_WithCardioOnly()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-200);
        var dateStr = date.ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(dateStr, "Running", "Zone2", 30, null, null, null, null, null));

        var response = await _client.GetAsync($"/api/v1/training-day?date={dateStr}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrainingDaySummaryResponse>();
        result.Should().NotBeNull();
        result!.CardioSessions.Should().BeGreaterThanOrEqualTo(1);
        result.Date.Should().Be(dateStr);
    }

    [Fact]
    public async Task GetSummary_Returns200_WithStrengthOnly()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-190);
        var dateStr = date.ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/training-day?date={dateStr}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrainingDaySummaryResponse>();
        result.Should().NotBeNull();
        result!.Sequencing.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummary_Returns200_WithBothCardioAndStrength()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-180);
        var dateStr = date.ToString("yyyy-MM-dd");

        // Log cardio for this date
        await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(dateStr, "Cycling", "Zone3", 45, null, null, null, null, null));

        var response = await _client.GetAsync($"/api/v1/training-day?date={dateStr}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrainingDaySummaryResponse>();
        result.Should().NotBeNull();
        result!.InterferenceLevel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSummary_IncludesSequencingInfo()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-170);
        var dateStr = date.ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(dateStr, "Running", "Zone4", 60, null, null, null, null, null));

        var response = await _client.GetAsync($"/api/v1/training-day?date={dateStr}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TrainingDaySummaryResponse>();
        result.Should().NotBeNull();
        result!.Sequencing.Should().NotBeNull();
        result.Sequencing.RecommendedOrder.Should().NotBeNullOrEmpty();
        result.Sequencing.Rationale.Should().NotBeNullOrEmpty();
    }
}
