using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class CoachAthleteConfiguration : IEntityTypeConfiguration<CoachAthlete>
{
    public void Configure(EntityTypeBuilder<CoachAthlete> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.InviteCode).HasMaxLength(6).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");
        builder.Property(c => c.Permissions).HasColumnType("jsonb");

        builder.HasIndex(c => c.InviteCode).IsUnique();
        builder.HasIndex(c => new { c.CoachId, c.Status });
        builder.HasIndex(c => new { c.AthleteId, c.Status });

        builder.HasOne(c => c.Coach)
            .WithMany(u => u.CoachRelationshipsAsCoach)
            .HasForeignKey(c => c.CoachId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Athlete)
            .WithMany(u => u.CoachRelationshipsAsAthlete)
            .HasForeignKey(c => c.AthleteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
