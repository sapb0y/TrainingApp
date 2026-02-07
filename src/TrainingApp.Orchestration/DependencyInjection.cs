using Microsoft.Extensions.DependencyInjection;
using TrainingApp.Orchestration.Services;

namespace TrainingApp.Orchestration;

public static class DependencyInjection
{
    public static IServiceCollection AddOrchestration(this IServiceCollection services)
    {
        services.AddHostedService<ExerciseSyncService>();
        services.AddHostedService<WorkoutReminderService>();
        services.AddHostedService<DailyMetricsJob>();

        return services;
    }
}
