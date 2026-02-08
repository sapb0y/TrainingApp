using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class PartnershipConfiguration : IEntityTypeConfiguration<Partnership>
{
    public void Configure(EntityTypeBuilder<Partnership> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.InviteCode).HasMaxLength(6).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(p => p.InviteCode).IsUnique();
        builder.HasIndex(p => new { p.RequesterId, p.Status });
        builder.HasIndex(p => new { p.ResponderId, p.Status });

        builder.HasOne(p => p.Requester)
            .WithMany(u => u.PartnershipsAsRequester)
            .HasForeignKey(p => p.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Responder)
            .WithMany(u => u.PartnershipsAsResponder)
            .HasForeignKey(p => p.ResponderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
