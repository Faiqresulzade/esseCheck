using EssayChecker.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class DeviceTrialConfiguration : IEntityTypeConfiguration<DeviceTrial>
{
    public void Configure(EntityTypeBuilder<DeviceTrial> builder)
    {
        builder.ToTable("DeviceTrials");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceIdHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(d => d.GrantedAt).IsRequired();

        // Qorumanın özü budur: bir cihaz = bir sətir. Paralel iki qeydiyyat gəlsə, ikincisi
        // bu indeksdə uğursuz olur və trial almır.
        builder.HasIndex(d => d.DeviceIdHash).IsUnique();

        // AppUser-ə FK QƏSDƏN yoxdur: hesab silinsə də cihaz qeydi qalmalıdır, əks halda
        // "hesabı sil, yenidən qeydiyyatdan keç" ilə qoruma keçilərdi.
    }
}
