using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(g => g.TargetValue).HasPrecision(10, 2);
        builder.Property(g => g.TargetUnit).HasMaxLength(50);
        builder.Property(g => g.StartValue).HasPrecision(10, 2);
        builder.Property(g => g.Notes).HasMaxLength(500);

        builder.Property(g => g.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(g => g.User)
            .WithMany(u => u.Goals)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(g => new { g.UserId, g.Status });
    }
}
