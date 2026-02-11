using System.Net;
using System.Text;
using FluentAssertions;

namespace TrainingApp.Integration.Tests.Endpoints;

public class WebhookEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WebhookEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StripeWebhook_MissingSignature_Returns400()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        // No Stripe-Signature header
        var response = await _client.PostAsync("/api/webhooks/stripe", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_InvalidSignature_Returns400()
    {
        var content = new StringContent("{\"type\":\"checkout.session.completed\"}", Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/stripe")
        {
            Content = content
        };
        request.Headers.Add("Stripe-Signature", "t=123,v1=invalid");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
