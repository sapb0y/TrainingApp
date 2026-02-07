using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class RecoveryLogConfiguration : IEntityTypeConfiguration<RecoveryLog>
{
    public void Configure(EntityTypeBuilder<RecoveryLog> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.SleepHours).HasPrecision(4, 2);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(r => r.User)
            .WithMany(u => u.RecoveryLogs)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.UserId, r.Date }).IsUnique();
    }
}
