using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data;

public class TrainingAppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public TrainingAppDbContext(DbContextOptions<TrainingAppDbContext> options) : base(options) { }

    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Workout> Workouts => Set<Workout>();
    public DbSet<WorkoutSet> WorkoutSets => Set<WorkoutSet>();
    public DbSet<Program> Programs => Set<Program>();
    public DbSet<ProgramPhase> ProgramPhases => Set<ProgramPhase>();
    public DbSet<ProgramWorkout> ProgramWorkouts => Set<ProgramWorkout>();
    public DbSet<ProgramExercise> ProgramExercises => Set<ProgramExercise>();
    public DbSet<MuscleVolumeTarget> MuscleVolumeTargets => Set<MuscleVolumeTarget>();
    public DbSet<AdaptationLog> AdaptationLogs => Set<AdaptationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrainingAppDbContext).Assembly);

        // Snake_case naming convention for all tables and columns
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()!));

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()!));
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
            }
        }
    }

    private static string ToSnakeCase(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + c.ToString() : c.ToString()))
            .ToLowerInvariant();
    }
}
