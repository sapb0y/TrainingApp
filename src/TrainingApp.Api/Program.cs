using System.Text;
using FluentValidation;
using HealthChecks.NpgSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TrainingApp.Api.Auth;
using TrainingApp.Api.Endpoints;
using TrainingApp.Api.Health;
using TrainingApp.Api.Middleware;
using TrainingApp.Api.Services;
using TrainingApp.Api.Validators;
using TrainingApp.Core.Configuration;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure;
using TrainingApp.Orchestration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOrchestration();

// HTTP Context accessor for CurrentUserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Authentication
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);
}
else
{
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };
        });
}

builder.Services.AddAuthorization();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateWorkoutRequestValidator>();

// OpenAPI + Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TrainingApp API", Version = "v1" });
});

// Health Checks
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddCheck<WgerHealthCheck>("wger");

var app = builder.Build();

// Exception handling middleware (must be first)
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TrainingApp API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapExerciseEndpoints();
app.MapWorkoutEndpoints();
app.MapProgramEndpoints();
app.MapFatigueEndpoints();
app.MapWeightEndpoints();
app.MapDeficitEndpoints();
app.MapNeatEndpoints();
app.MapMetabolismEndpoints();
app.MapCardioEndpoints();
app.MapTrainingDayEndpoints();
app.MapChartEndpoints();
app.MapDashboardEndpoints();
app.MapGoalEndpoints();
app.MapInsightEndpoints();
app.MapExportEndpoints();
app.MapPartnerEndpoints();
app.MapSharedSessionEndpoints();
app.MapCoachEndpoints();
app.MapCoachDashboardEndpoints();
app.MapCoachActionEndpoints();
app.MapSubscriptionEndpoints();
app.MapCoachApplicationEndpoints();
app.MapAdminEndpoints();

app.Run();

// Required for integration tests
public partial class Program { }
