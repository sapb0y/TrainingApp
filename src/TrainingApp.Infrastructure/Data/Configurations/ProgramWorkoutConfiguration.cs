using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class ProgramWorkoutConfiguration : IEntityTypeConfiguration<ProgramWorkout>
{
    public void Configure(EntityTypeBuilder<ProgramWorkout> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Focus)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(w => w.ProgramPhase)
            .WithMany(p => p.Workouts)
            .HasForeignKey(w => w.ProgramPhaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.ProgramPhaseId, w.OrderIndex });
    }
}
