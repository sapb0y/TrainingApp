using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.Detail).HasMaxLength(2000);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(a => a.AdminUserId);
        builder.HasIndex(a => a.CreatedAt).IsDescending();
    }
}
