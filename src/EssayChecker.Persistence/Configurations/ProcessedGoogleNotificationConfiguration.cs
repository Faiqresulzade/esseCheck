using EssayChecker.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class ProcessedGoogleNotificationConfiguration : IEntityTypeConfiguration<ProcessedGoogleNotification>
{
    public void Configure(EntityTypeBuilder<ProcessedGoogleNotification> builder)
    {
        builder.ToTable("ProcessedGoogleNotifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.MessageId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.ProcessedAt).IsRequired();

        builder.HasIndex(n => n.MessageId).IsUnique();
    }
}
