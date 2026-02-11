using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .WithTags("Webhooks")
            .AllowAnonymous();

        group.MapPost("/stripe", HandleStripeWebhook)
            .WithName("HandleStripeWebhook")
            .WithSummary("Handle Stripe webhook events");
    }

    private static async Task<IResult> HandleStripeWebhook(
        HttpContext context,
        IStripeWebhookHandler webhookHandler,
        CancellationToken ct)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body);
        var payload = await reader.ReadToEndAsync(ct);

        var signature = context.Request.Headers["Stripe-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(signature))
            return Results.BadRequest(new { error = "Missing Stripe-Signature header" });

        try
        {
            await webhookHandler.HandleAsync(payload, signature, ct);
            return Results.Ok();
        }
        catch (Stripe.StripeException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
