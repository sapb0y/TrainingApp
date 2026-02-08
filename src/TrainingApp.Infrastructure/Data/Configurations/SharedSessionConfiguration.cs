using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class SharedSessionConfiguration : IEntityTypeConfiguration<SharedSession>
{
    public void Configure(EntityTypeBuilder<SharedSession> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(s => new { s.PartnershipId, s.ScheduledDate });

        builder.HasOne(s => s.Partnership)
            .WithMany(p => p.SharedSessions)
            .HasForeignKey(s => s.PartnershipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.WorkoutA)
            .WithMany()
            .HasForeignKey(s => s.WorkoutAId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.WorkoutB)
            .WithMany()
            .HasForeignKey(s => s.WorkoutBId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
