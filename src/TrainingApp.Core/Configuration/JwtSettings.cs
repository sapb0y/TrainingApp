namespace TrainingApp.Core.Configuration;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public required string Secret { get; set; }
    public string Issuer { get; set; } = "FitSpiralS";
    public string Audience { get; set; } = "FitSpiralS";
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
