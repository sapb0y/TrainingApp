namespace TrainingApp.Core.Interfaces;

public interface IStripeWebhookHandler
{
    Task HandleAsync(string payload, string signatureHeader, CancellationToken ct = default);
}
