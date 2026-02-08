using System.Net;
using FluentAssertions;

namespace TrainingApp.Integration.Tests.Endpoints;

public class ExportEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ExportEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExportWorkouts_ReturnsCsv()
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/export/workouts?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().StartWith("Date,Exercise,Set#,Weight,Reps,RPE,e1RM,Notes");
    }

    [Fact]
    public async Task ExportWeightLogs_ReturnsCsv()
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/export/weight?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().StartWith("Date,WeightKg,MA7d,MA30d,WeeklyRate");
    }

    [Fact]
    public async Task ExportCardio_ReturnsCsv()
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/export/cardio?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().StartWith("Date,Modality,Zone,Duration,Distance,AvgHR,TRIMP");
    }
}
