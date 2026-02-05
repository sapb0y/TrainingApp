using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class WorkoutSetConfiguration : IEntityTypeConfiguration<WorkoutSet>
{
    public void Configure(EntityTypeBuilder<WorkoutSet> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TargetWeight)
            .HasPrecision(8, 2);

        builder.Property(s => s.ActualWeight)
            .HasPrecision(8, 2);

        builder.Property(s => s.Rpe)
            .HasPrecision(3, 1);

        builder.Ignore(s => s.EstimatedOneRepMax);

        builder.HasOne(s => s.Workout)
            .WithMany(w => w.Sets)
            .HasForeignKey(s => s.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Exercise)
            .WithMany(e => e.WorkoutSets)
            .HasForeignKey(s => s.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.WorkoutId, s.SetNumber });

        builder.HasIndex(s => s.ExerciseId);
    }
}
