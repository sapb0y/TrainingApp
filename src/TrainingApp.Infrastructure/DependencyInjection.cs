using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Polly;
using Polly.Extensions.Http;
using Refit;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;
using TrainingApp.Infrastructure.External.Wger;
using TrainingApp.Infrastructure.Services;

namespace TrainingApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<TrainingAppDbContext>(options =>
            options.UseNpgsql(dataSource));

        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<TrainingAppDbContext>();

        // Memory cache
        services.AddMemoryCache();

        // Wger API client with Polly policies
        var wgerBaseUrl = configuration["Wger:BaseUrl"] ?? "https://wger.de/api/v2";

        services.AddRefitClient<IWgerApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(wgerBaseUrl))
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        services.AddScoped<IWgerClient, WgerClient>();
        services.AddScoped<IExerciseCacheService, ExerciseCacheService>();
        services.AddScoped<IProgramGeneratorService, ProgramGeneratorService>();
        services.AddScoped<IAutoregulationExecutionService, AutoregulationExecutionService>();
        services.AddScoped<IFatigueModelService, FatigueModelService>();
        services.AddScoped<IWeightTrackingService, WeightTrackingService>();
        services.AddScoped<IDeficitPhaseService, DeficitPhaseService>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
