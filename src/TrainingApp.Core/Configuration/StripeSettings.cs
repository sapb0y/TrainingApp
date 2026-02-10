namespace TrainingApp.Core.Configuration;

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public StripePriceIds PriceIds { get; set; } = new();
}

public class StripePriceIds
{
    public string AthleteMonthly { get; set; } = string.Empty;
    public string AthleteAnnual { get; set; } = string.Empty;
    public string CompetitorMonthly { get; set; } = string.Empty;
    public string CompetitorAnnual { get; set; } = string.Empty;
    public string CoachMonthly { get; set; } = string.Empty;
    public string CoachAnnual { get; set; } = string.Empty;
}
