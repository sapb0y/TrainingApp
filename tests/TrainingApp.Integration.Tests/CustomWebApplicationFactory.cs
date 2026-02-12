using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure.Data;

namespace TrainingApp.Integration.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly Guid TempUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestUserBId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid TestCoachId = Guid.Parse("00000000-0000-0000-0000-000000000003");

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
        // Provide JWT secret for auth endpoint tests (no longer in appsettings.json)
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "TestOnlySecret_ForIntegrationTests_MinLength32Chars!!",
                ["Stripe:SecretKey"] = "sk_test_fake",
                ["Stripe:PublishableKey"] = "pk_test_fake",
                ["Stripe:WebhookSecret"] = "whsec_test_fake",
                ["Email:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TrainingAppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add DbContext with test container connection string
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<TrainingAppDbContext>(options =>
                options.UseNpgsql(dataSource)
                    .ConfigureWarnings(w => w.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            // Replace IPaymentService with a fake for integration tests
            var paymentDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPaymentService));
            if (paymentDescriptor != null)
                services.Remove(paymentDescriptor);
            services.AddScoped<IPaymentService, FakePaymentService>();

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();
            db.Database.Migrate();
            SeedTestUser(db);
        });

        builder.UseEnvironment("Testing");
    }

    public HttpClient CreatePartnerClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", TestUserBId.ToString());
        return client;
    }

    public HttpClient CreateCoachClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", TestCoachId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-UserRole", "Coach");
        return client;
    }

    public HttpClient CreateAthleteClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-SubscriptionTier", "Athlete");
        return client;
    }

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserRole", "Admin");
        return client;
    }

    private static void SeedTestUser(TrainingAppDbContext db)
    {
        if (!db.Users.Any(u => u.Id == TempUserId))
        {
            db.Users.Add(new User
            {
                Id = TempUserId,
                UserName = "testuser",
                NormalizedUserName = "TESTUSER",
                Email = "test@example.com",
                NormalizedEmail = "TEST@EXAMPLE.COM",
                DisplayName = "Test User",
                SecurityStamp = Guid.NewGuid().ToString()
            });
        }

        if (!db.Users.Any(u => u.Id == TestUserBId))
        {
            db.Users.Add(new User
            {
                Id = TestUserBId,
                UserName = "testpartner",
                NormalizedUserName = "TESTPARTNER",
                Email = "partner@example.com",
                NormalizedEmail = "PARTNER@EXAMPLE.COM",
                DisplayName = "Test Partner",
                SecurityStamp = Guid.NewGuid().ToString()
            });
        }

        if (!db.Users.Any(u => u.Id == TestCoachId))
        {
            db.Users.Add(new User
            {
                Id = TestCoachId,
                UserName = "testcoach",
                NormalizedUserName = "TESTCOACH",
                Email = "coach@example.com",
                NormalizedEmail = "COACH@EXAMPLE.COM",
                DisplayName = "Test Coach",
                SecurityStamp = Guid.NewGuid().ToString()
            });
        }

        db.SaveChanges();
    }
}

public class FakePaymentService : IPaymentService
{
    public Task<string> CreateOrGetCustomerAsync(Guid userId, string email, string name, CancellationToken ct = default)
        => Task.FromResult($"cus_fake_{userId:N}");

    public Task<CheckoutResult> CreateCheckoutSessionAsync(Guid userId, string stripeCustomerId, SubscriptionTier tier, BillingInterval interval, string successUrl, string cancelUrl, CancellationToken ct = default)
        => Task.FromResult(new CheckoutResult("cs_fake_session", "https://checkout.stripe.com/fake"));

    public Task<string> CreatePortalSessionAsync(string stripeCustomerId, string returnUrl, CancellationToken ct = default)
        => Task.FromResult("https://billing.stripe.com/fake-portal");

    public Task CancelSubscriptionAsync(string stripeSubscriptionId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<string> CreateSetupIntentAsync(string stripeCustomerId, CancellationToken ct = default)
        => Task.FromResult("seti_fake_client_secret");

    public Task<string> CreateSubscriptionWithTrialAsync(string stripeCustomerId, string priceId, int trialDays, CancellationToken ct = default)
        => Task.FromResult("sub_fake_trial");
}
