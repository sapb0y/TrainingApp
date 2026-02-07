using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class DailyMetricsConfiguration : IEntityTypeConfiguration<DailyMetrics>
{
    public void Configure(EntityTypeBuilder<DailyMetrics> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Trimp).HasPrecision(12, 2);
        builder.Property(d => d.Ctl).HasPrecision(12, 2);
        builder.Property(d => d.Atl).HasPrecision(12, 2);
        builder.Property(d => d.Tsb).HasPrecision(12, 2);
        builder.Property(d => d.TotalVolume).HasPrecision(14, 2);
        builder.Property(d => d.AverageSessionRpe).HasPrecision(4, 2);
        builder.Property(d => d.ReadinessScore).HasPrecision(4, 2);

        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(d => d.User)
            .WithMany(u => u.DailyMetrics)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.UserId, d.Date }).IsUnique();
    }
}
