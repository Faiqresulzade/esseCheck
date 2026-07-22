using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Plan)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Platform)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.PurchaseToken)
            .HasMaxLength(4000);

        builder.Property(s => s.PurchaseTokenHash)
            .HasMaxLength(64);

        builder.Property(s => s.ProductId)
            .HasMaxLength(200);

        builder.Property(s => s.LinkedPurchaseToken)
            .HasMaxLength(4000);

        builder.Property(s => s.StartDate).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.UserId, s.IsActive });
        builder.HasIndex(s => s.PurchaseTokenHash);
    }
}
