using HealthChecks.NpgSql;
using TrainingApp.Api.Endpoints;
using TrainingApp.Api.Health;
using TrainingApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

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
