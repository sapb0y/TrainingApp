using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class DeficitPhaseConfiguration : IEntityTypeConfiguration<DeficitPhase>
{
    public void Configure(EntityTypeBuilder<DeficitPhase> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.StartWeightKg).HasPrecision(6, 2);
        builder.Property(d => d.TargetWeightKg).HasPrecision(6, 2);
        builder.Property(d => d.WeeklyRateKg).HasPrecision(4, 2);
        builder.Property(d => d.CurrentAdaptationPercent).HasPrecision(5, 2);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.Property(d => d.Strategy).HasConversion<string>();
        builder.Property(d => d.Status).HasConversion<string>();

        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.User)
            .WithMany(u => u.DeficitPhases)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.UserId, d.Status });
    }
}
