using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TrainingAppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext with test container connection string
            services.AddDbContext<TrainingAppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Testing");
    }
}
