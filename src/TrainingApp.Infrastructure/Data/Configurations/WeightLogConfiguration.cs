using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class WeightLogConfiguration : IEntityTypeConfiguration<WeightLog>
{
    public void Configure(EntityTypeBuilder<WeightLog> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WeightKg).HasPrecision(6, 2);
        builder.Property(w => w.MovingAverage7d).HasPrecision(6, 2);
        builder.Property(w => w.MovingAverage30d).HasPrecision(6, 2);
        builder.Property(w => w.WeeklyRateKg).HasPrecision(6, 2);
        builder.Property(w => w.Notes).HasMaxLength(500);

        builder.Property(w => w.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(w => w.User)
            .WithMany(u => u.WeightLogs)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.UserId, w.Date }).IsUnique();
    }
}
