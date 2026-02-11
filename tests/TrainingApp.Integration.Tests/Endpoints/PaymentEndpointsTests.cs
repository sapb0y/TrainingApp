using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TrainingApp.Integration.Tests.Endpoints;

public class PaymentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PaymentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCheckoutSession_InvalidTier_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payment/checkout-session", new
        {
            Tier = "SuperHero",
            Interval = "Monthly",
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCheckoutSession_InvalidInterval_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payment/checkout-session", new
        {
            Tier = "Competitor",
            Interval = "Weekly",
            SuccessUrl = "https://example.com/success",
            CancelUrl = "https://example.com/cancel"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePortalSession_NoStripeCustomer_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payment/portal-session", new
        {
            ReturnUrl = "https://example.com/dashboard"
        });

        // User has no Stripe customer ID → 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCheckoutSession_MissingUrl_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/payment/checkout-session", new
        {
            Tier = "Competitor",
            Interval = "Monthly",
            SuccessUrl = "",
            CancelUrl = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
