using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class MuscleVolumeTargetConfiguration : IEntityTypeConfiguration<MuscleVolumeTarget>
{
    public void Configure(EntityTypeBuilder<MuscleVolumeTarget> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.MuscleGroup)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(v => v.ProgramPhase)
            .WithMany(p => p.VolumeTargets)
            .HasForeignKey(v => v.ProgramPhaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.ProgramPhaseId, v.MuscleGroup })
            .IsUnique();
    }
}
