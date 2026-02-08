using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class CardioEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CardioEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LogCardio_Returns200_WithValidData()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(today, "Running", "Zone2", 30, 5.0m, 145, 165, null, "Easy run"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CardioSessionResponse>();
        result.Should().NotBeNull();
        result!.Modality.Should().Be("Running");
        result.DurationMinutes.Should().Be(30);
    }

    [Fact]
    public async Task LogCardio_CalculatesTrimp()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-100).ToString("yyyy-MM-dd");
        var response = await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(date, "Running", "Zone2", 30, null, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CardioSessionResponse>();
        result.Should().NotBeNull();
        result!.CardioTrimp.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCardioHistory_Returns200_WithDateRange()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90);
        var dateStr = date.ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(dateStr, "Cycling", "Zone3", 45, 15.0m, null, null, null, null));

        var response = await _client.GetAsync($"/api/v1/cardio?from={dateStr}&to={dateStr}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CardioSessionListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCardioSession_Returns200_ById()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-80).ToString("yyyy-MM-dd");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(date, "Swimming", "Zone1", 20, null, null, null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<CardioSessionResponse>();

        var response = await _client.GetAsync($"/api/v1/cardio/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CardioSessionResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task DeleteCardio_Returns200_WhenExists()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-70).ToString("yyyy-MM-dd");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(date, "Walking", "Zone1", 60, null, null, null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<CardioSessionResponse>();

        var response = await _client.DeleteAsync($"/api/v1/cardio/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted
        var getResponse = await _client.GetAsync($"/api/v1/cardio/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWeeklyCardioSummary_Returns200()
    {
        var weekStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-60);
        var dateStr = weekStart.ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(dateStr, "Running", "Zone2", 30, 5.0m, null, null, null, null));
        await _client.PostAsJsonAsync("/api/v1/cardio",
            new LogCardioRequest(weekStart.AddDays(2).ToString("yyyy-MM-dd"), "Cycling", "Zone3", 45, 15.0m, null, null, null, null));

        var response = await _client.GetAsync($"/api/v1/cardio/weekly?weekStart={dateStr}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WeeklyCardioSummaryResponse>();
        result.Should().NotBeNull();
        result!.TotalSessions.Should().BeGreaterThanOrEqualTo(1);
    }
}
