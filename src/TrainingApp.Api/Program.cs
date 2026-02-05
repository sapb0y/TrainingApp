using FluentValidation;
using HealthChecks.NpgSql;
using TrainingApp.Api.Endpoints;
using TrainingApp.Api.Health;
using TrainingApp.Api.Middleware;
using TrainingApp.Api.Services;
using TrainingApp.Api.Validators;
using TrainingApp.Core.Interfaces;
using TrainingApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// HTTP Context accessor for CurrentUserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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

app.MapHealthChecks("/health");
app.MapExerciseEndpoints();
app.MapWorkoutEndpoints();

app.Run();

// Required for integration tests
public partial class Program { }
