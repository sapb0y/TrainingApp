using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class ProgramPhaseConfiguration : IEntityTypeConfiguration<ProgramPhase>
{
    public void Configure(EntityTypeBuilder<ProgramPhase> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.VolumeMultiplier)
            .HasPrecision(4, 2);

        builder.Property(p => p.IntensityMultiplier)
            .HasPrecision(4, 2);

        builder.HasOne(p => p.Program)
            .WithMany(pr => pr.Phases)
            .HasForeignKey(p => p.ProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ProgramId, p.OrderIndex });
    }
}
