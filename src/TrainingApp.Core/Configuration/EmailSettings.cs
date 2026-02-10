namespace TrainingApp.Core.Configuration;

public class EmailSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@fitspirals.com";
    public string FromName { get; set; } = "FitSpiralS";
    public bool Enabled { get; set; }
}
