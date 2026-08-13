using EssayChecker.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false);

        builder.Property(x => x.ReferralCode)
            .HasMaxLength(10);

        // Filtered unique index (yalnız NULL olmayan dəyərlər üçün) — kod lazy-generated olduğu
        // üçün çoxlu istifadəçi eyni vaxtda NULL ola bilər, bu, adi unikal indekslə toqquşardı.
        builder.HasIndex(x => x.ReferralCode)
            .IsUnique()
            .HasFilter("\"ReferralCode\" IS NOT NULL");

        builder.HasIndex(x => x.ReferredByUserId);

        builder.Property(x => x.ReferralRewardGranted)
            .HasDefaultValue(false);
    }
}
