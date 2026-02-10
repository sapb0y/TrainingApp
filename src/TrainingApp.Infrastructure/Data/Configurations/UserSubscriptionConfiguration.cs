using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TrainingApp.Core.Entities;

namespace TrainingApp.Infrastructure.Data.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Tier).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Interval).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.StripeCustomerId).HasMaxLength(255);
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(255);
        builder.Property(s => s.CancelledAt);
        builder.Property(s => s.PaymentFailedAt);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(s => s.UserId).IsUnique();
        builder.HasIndex(s => s.StripeCustomerId).HasFilter("stripe_customer_id IS NOT NULL");
        builder.HasIndex(s => s.StripeSubscriptionId).HasFilter("stripe_subscription_id IS NOT NULL");

        builder.HasOne(s => s.User)
            .WithOne(u => u.Subscription)
            .HasForeignKey<UserSubscription>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
