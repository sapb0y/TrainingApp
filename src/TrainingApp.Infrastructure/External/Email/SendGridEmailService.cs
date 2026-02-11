using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using TrainingApp.Core.Configuration;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Infrastructure.External.Email;

public class SendGridEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IOptions<EmailSettings> settings, ILogger<SendGridEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SendWelcomeAsync(string email, string name, CancellationToken ct = default) =>
        SendAsync(email, "Welcome to FitSpiralS!", BuildHtml(
            $"Welcome, {name}!",
            "Your 30-day free trial has started. You have full access to Competitor-tier features. Explore periodized training, autoregulation, and fatigue modeling."));

    public Task SendTrialExpiringAsync(string email, string name, int daysRemaining, CancellationToken ct = default) =>
        SendAsync(email, $"Your trial expires in {daysRemaining} day(s)", BuildHtml(
            $"Hi {name},",
            $"Your free trial expires in {daysRemaining} day(s). Subscribe now to keep your Competitor features, or you'll be downgraded to the free Athlete tier."));

    public Task SendSubscriptionConfirmedAsync(string email, string name, string tier, CancellationToken ct = default) =>
        SendAsync(email, "Subscription confirmed!", BuildHtml(
            $"Thanks, {name}!",
            $"Your {tier} subscription is now active. Enjoy all the features that come with your plan."));

    public Task SendPaymentFailedAsync(string email, string name, CancellationToken ct = default) =>
        SendAsync(email, "Payment failed — action required", BuildHtml(
            $"Hi {name},",
            "We couldn't process your latest payment. Please update your payment method in the billing portal to avoid losing access to your current tier."));

    public Task SendCoachApprovedAsync(string email, string name, CancellationToken ct = default) =>
        SendAsync(email, "Coach application approved!", BuildHtml(
            $"Congrats, {name}!",
            "Your coach application has been approved. You now have 3 months of free Coach-tier access. Reach 20 active clients to keep Coach tier free forever."));

    public Task SendCoachRejectedAsync(string email, string name, string? reason, CancellationToken ct = default) =>
        SendAsync(email, "Coach application update", BuildHtml(
            $"Hi {name},",
            $"Your coach application was not approved.{(reason is not null ? $" Reason: {reason}" : "")} You can reapply at any time."));

    private async Task SendAsync(string toEmail, string subject, string htmlContent)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email disabled — skipping: {Subject} to {Email}", subject, toEmail);
            return;
        }

        try
        {
            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(_settings.FromEmail, _settings.FromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("SendGrid returned {StatusCode} for {Subject} to {Email}", response.StatusCode, subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email: {Subject} to {Email}", subject, toEmail);
        }
    }

    private static string BuildHtml(string heading, string body) =>
        $"""
        <div style="font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
            <h2 style="color: #2563eb;">{heading}</h2>
            <p style="color: #374151; line-height: 1.6;">{body}</p>
            <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;" />
            <p style="color: #9ca3af; font-size: 12px;">FitSpiralS — Adaptive Training Platform</p>
        </div>
        """;
}
