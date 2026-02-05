using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PrimaryMuscles)
            .HasColumnType("jsonb");

        builder.Property(e => e.SecondaryMuscles)
            .HasColumnType("jsonb");

        builder.Property(e => e.Equipment)
            .HasColumnType("jsonb");

        builder.HasIndex(e => e.WgerId)
            .IsUnique()
            .HasFilter("wger_id IS NOT NULL");

        builder.HasIndex(e => e.Name);

        builder.HasIndex(e => e.Category);
    }
}
