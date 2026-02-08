using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class ChartEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ChartEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStrengthChart_Returns200()
    {
        var exerciseId = Guid.NewGuid();
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/charts/strength?exerciseId={exerciseId}&from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ChartDataResponse>();
        result.Should().NotBeNull();
        result!.Series.Should().NotBeNull();
        result.Statistics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBodyWeightChart_Returns200()
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/charts/body-weight?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MultiSeriesChartResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Body Weight");
    }

    [Fact]
    public async Task GetVolumeChart_Returns200()
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/charts/volume?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MultiSeriesChartResponse>();
        result.Should().NotBeNull();
        result!.Title.Should().Be("Training Volume");
    }

    [Fact]
    public async Task GetDashboard_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>();
        result.Should().NotBeNull();
        result!.Strength.Should().NotBeNull();
        result.BodyWeight.Should().NotBeNull();
        result.Cardio.Should().NotBeNull();
        result.Fatigue.Should().NotBeNull();
        result.Volume.Should().NotBeNull();
    }
}
