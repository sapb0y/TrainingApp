using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class ProgramExerciseConfiguration : IEntityTypeConfiguration<ProgramExercise>
{
    public void Configure(EntityTypeBuilder<ProgramExercise> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.IntensityPercentage)
            .HasPrecision(5, 2);

        builder.Property(e => e.TargetRpe)
            .HasPrecision(3, 1);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.SupersetGroup)
            .HasMaxLength(50);

        builder.HasOne(e => e.ProgramWorkout)
            .WithMany(w => w.Exercises)
            .HasForeignKey(e => e.ProgramWorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Exercise)
            .WithMany(ex => ex.ProgramExercises)
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ProgramWorkoutId, e.OrderIndex });
        builder.HasIndex(e => e.ExerciseId);
    }
}
