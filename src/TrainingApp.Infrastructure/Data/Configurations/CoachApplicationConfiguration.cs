using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class CoachApplicationConfiguration : IEntityTypeConfiguration<CoachApplication>
{
    public void Configure(EntityTypeBuilder<CoachApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Credentials).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.BusinessGoal).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.AdditionalInfo).HasMaxLength(2000);
        builder.Property(a => a.ReviewNotes).HasMaxLength(2000);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(a => new { a.UserId, a.Status });

        builder.HasOne(a => a.User)
            .WithMany(u => u.CoachApplications)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
