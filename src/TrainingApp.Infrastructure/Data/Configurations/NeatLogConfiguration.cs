using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class NeatLogConfiguration : IEntityTypeConfiguration<NeatLog>
{
    public void Configure(EntityTypeBuilder<NeatLog> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.EstimatedNeatKcal).HasPrecision(8, 2);
        builder.Property(n => n.CompensationPercent).HasPrecision(5, 2);
        builder.Property(n => n.Notes).HasMaxLength(500);

        builder.Property(n => n.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(n => n.User)
            .WithMany(u => u.NeatLogs)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.UserId, n.Date }).IsUnique();
    }
}
