using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class GoalCheckpointConfiguration : IEntityTypeConfiguration<GoalCheckpoint>
{
    public void Configure(EntityTypeBuilder<GoalCheckpoint> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Value).HasPrecision(10, 2);
        builder.Property(c => c.PercentComplete).HasPrecision(5, 1);
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(c => c.Goal)
            .WithMany(g => g.Checkpoints)
            .HasForeignKey(c => c.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.GoalId, c.Date });
    }
}
