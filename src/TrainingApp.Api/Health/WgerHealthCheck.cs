using Microsoft.Extensions.Diagnostics.HealthChecks;
using TrainingApp.Core.Interfaces;

namespace TrainingApp.Api.Health;

public class WgerHealthCheck : IHealthCheck
{
    private readonly IWgerClient _wgerClient;

    public WgerHealthCheck(IWgerClient wgerClient)
    {
        _wgerClient = wgerClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var response = await _wgerClient.GetCategoriesAsync(ct);
            return response.Count > 0
                ? HealthCheckResult.Healthy("wger API is responding")
                : HealthCheckResult.Degraded("wger API returned empty categories");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("wger API is not responding", ex);
        }
    }
}
