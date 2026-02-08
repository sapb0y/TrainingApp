using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrainingApp.Api.Contracts;

namespace TrainingApp.Integration.Tests.Endpoints;

public class InsightEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InsightEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GenerateInsights_Returns200()
    {
        var response = await _client.PostAsync("/api/v1/insights/generate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InsightListResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInsightHistory_Returns200()
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30).ToString("yyyy-MM-dd");
        var to = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync($"/api/v1/insights?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InsightListResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateInsights_DeduplicatesExisting()
    {
        // Generate twice
        await _client.PostAsync("/api/v1/insights/generate", null);
        var response = await _client.PostAsync("/api/v1/insights/generate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InsightListResponse>();
        result.Should().NotBeNull();
        // Second run should produce 0 new insights (all deduped)
        result!.TotalCount.Should().Be(0);
    }
}
