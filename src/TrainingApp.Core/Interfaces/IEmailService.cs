namespace TrainingApp.Core.Interfaces;

public interface IEmailService
{
    Task SendWelcomeAsync(string email, string name, CancellationToken ct = default);
    Task SendTrialExpiringAsync(string email, string name, int daysRemaining, CancellationToken ct = default);
    Task SendSubscriptionConfirmedAsync(string email, string name, string tier, CancellationToken ct = default);
    Task SendPaymentFailedAsync(string email, string name, CancellationToken ct = default);
    Task SendCoachApprovedAsync(string email, string name, CancellationToken ct = default);
    Task SendCoachRejectedAsync(string email, string name, string? reason, CancellationToken ct = default);
}
