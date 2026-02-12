using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;
using TrainingApp.Core.Entities;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure;
using TrainingApp.Infrastructure.Data;
using TrainingApp.Web.Components;
using TrainingApp.Orchestration;
using TrainingApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Static web assets manifest (needed for non-Development environments with dotnet run)
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

// Shared infrastructure: DB, cache, domain services
builder.Services.AddInfrastructureData(builder.Configuration);

// Identity with cookie auth (full Identity, not IdentityCore — gives us SignInManager)
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<TrainingAppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, WebCurrentUserService>();

builder.Services.AddOrchestration();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Seed admin user
await SeedAdminAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/logout", async (SignInManager<User> sm) =>
{
    await sm.SignOutAsync();
    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task SeedAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var db = scope.ServiceProvider.GetRequiredService<TrainingAppDbContext>();

    string[] roles = ["Admin", "Coach", "Athlete"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
    }

    var seedAccounts = new[]
    {
        ("admin@fitspirals.com", "Fs!2026$Adm1n", "Admin", "Admin", (SubscriptionTier?)null),
        ("coach@fitspirals.com", "Fs!2026$Coach", "Coach Demo", "Coach", (SubscriptionTier?)SubscriptionTier.Coach),
        ("competitor@fitspirals.com", "Fs!2026$Comp", "Competitor Demo", "Athlete", (SubscriptionTier?)SubscriptionTier.Competitor),
        ("athlete@fitspirals.com", "Fs!2026$Athl", "Athlete Demo", "Athlete", (SubscriptionTier?)SubscriptionTier.Athlete),
    };

    foreach (var (email, password, displayName, role, tier) in seedAccounts)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null) continue;

        var user = new User
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded) continue;

        await userManager.AddToRoleAsync(user, role);

        if (tier.HasValue)
        {
            db.UserSubscriptions.Add(new UserSubscription
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Tier = tier.Value,
                Status = SubscriptionStatus.Active,
                StartDate = DateTimeOffset.UtcNow,
                CurrentPeriodEnd = DateTimeOffset.UtcNow.AddYears(1),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    await db.SaveChangesAsync();
}
