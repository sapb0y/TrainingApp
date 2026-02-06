using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class AdaptationLogConfiguration : IEntityTypeConfiguration<AdaptationLog>
{
    public void Configure(EntityTypeBuilder<AdaptationLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.RuleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Scope)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.InputSummary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.OutputSummary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasOne(a => a.Workout)
            .WithMany()
            .HasForeignKey(a => a.WorkoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.WorkoutSet)
            .WithMany()
            .HasForeignKey(a => a.WorkoutSetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.WorkoutId, a.CreatedAt });
    }
}
