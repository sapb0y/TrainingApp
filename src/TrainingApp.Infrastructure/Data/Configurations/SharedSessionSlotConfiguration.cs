using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class SharedSessionSlotConfiguration : IEntityTypeConfiguration<SharedSessionSlot>
{
    public void Configure(EntityTypeBuilder<SharedSessionSlot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserAAction).HasMaxLength(50);
        builder.Property(s => s.UserBAction).HasMaxLength(50);
        builder.Property(s => s.EquipmentNote).HasMaxLength(500);

        builder.HasIndex(s => new { s.SharedSessionId, s.SlotOrder });

        builder.HasOne(s => s.SharedSession)
            .WithMany(ss => ss.Slots)
            .HasForeignKey(s => s.SharedSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
