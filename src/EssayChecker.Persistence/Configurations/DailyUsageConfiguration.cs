using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class DailyUsageConfiguration : IEntityTypeConfiguration<DailyUsage>
{
    public void Configure(EntityTypeBuilder<DailyUsage> builder)
    {
        builder.ToTable("DailyUsages");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UsageDate).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Bir istifadəçi üçün gündə bir sətir.
        builder.HasIndex(u => new { u.UserId, u.UsageDate }).IsUnique();
    }
}
