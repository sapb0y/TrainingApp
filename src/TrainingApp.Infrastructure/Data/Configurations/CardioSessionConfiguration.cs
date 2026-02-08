using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class CardioSessionConfiguration : IEntityTypeConfiguration<CardioSession>
{
    public void Configure(EntityTypeBuilder<CardioSession> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Modality).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.Zone).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.DistanceKm).HasPrecision(6, 2);
        builder.Property(c => c.CardioTrimp).HasPrecision(6, 2);
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(c => c.User)
            .WithMany(u => u.CardioSessions)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.UserId, c.Date, c.Modality, c.StartedAt });
    }
}
