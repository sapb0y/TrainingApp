using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class WeightEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WeightEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LogWeight_Returns200_WithValidData()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var response = await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(today, 85.0m, "Morning weigh-in"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WeightLogResponse>();
        result.Should().NotBeNull();
        result!.WeightKg.Should().Be(85.0m);
        result.Date.Should().Be(today);
    }

    [Fact]
    public async Task LogWeight_UpsertsForSameDate()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-50).ToString("yyyy-MM-dd");

        var response1 = await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(date, 84.0m, null));
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await response1.Content.ReadFromJsonAsync<WeightLogResponse>();

        var response2 = await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(date, 84.5m, "Updated"));
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await response2.Content.ReadFromJsonAsync<WeightLogResponse>();

        second!.Id.Should().Be(first!.Id);
        second.WeightKg.Should().Be(84.5m);
    }

    [Fact]
    public async Task GetWeightHistory_Returns200_WithDateRange()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-40).ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(date, 83.0m, null));

        var response = await _client.GetAsync($"/api/v1/weight?from={date}&to={date}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WeightLogListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLatestWeight_Returns200_AfterLogging()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(date, 82.0m, null));

        var response = await _client.GetAsync("/api/v1/weight/latest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WeightLogResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProjection_Returns200_AfterLogging()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-35).ToString("yyyy-MM-dd");
        await _client.PostAsJsonAsync("/api/v1/weight",
            new LogWeightRequest(date, 80.0m, null));

        var response = await _client.GetAsync("/api/v1/weight/projection?weeks=4");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<WeightProjectionResponse>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(4);
    }
}
