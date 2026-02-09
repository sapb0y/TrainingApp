using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class CoachNoteConfiguration : IEntityTypeConfiguration<CoachNote>
{
    public void Configure(EntityTypeBuilder<CoachNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Content).HasMaxLength(500).IsRequired();
        builder.Property(n => n.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(n => new { n.AthleteId, n.CreatedAt });

        builder.HasOne(n => n.CoachAthlete)
            .WithMany(c => c.Notes)
            .HasForeignKey(n => n.CoachAthleteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Workout)
            .WithMany()
            .HasForeignKey(n => n.WorkoutId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.WorkoutSet)
            .WithMany()
            .HasForeignKey(n => n.WorkoutSetId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
