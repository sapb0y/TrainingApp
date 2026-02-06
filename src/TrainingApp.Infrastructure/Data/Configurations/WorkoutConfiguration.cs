using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Notes)
            .HasMaxLength(2000);

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(w => w.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasOne(w => w.User)
            .WithMany(u => u.Workouts)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Program)
            .WithMany(p => p.Workouts)
            .HasForeignKey(w => w.ProgramId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(w => new { w.UserId, w.ScheduledAt });

        builder.HasIndex(w => w.Status);

        builder.HasIndex(w => w.ProgramId)
            .HasFilter("program_id IS NOT NULL");
    }
}
