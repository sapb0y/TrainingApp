using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class InsightConfiguration : IEntityTypeConfiguration<Insight>
{
    public void Configure(EntityTypeBuilder<Insight> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.RuleName).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Category).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Severity).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Title).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Message).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.RelatedEntityType).HasMaxLength(100);

        builder.Property(i => i.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(i => i.User)
            .WithMany(u => u.Insights)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.UserId, i.GeneratedDate });
        builder.HasIndex(i => new { i.UserId, i.Category });
    }
}
