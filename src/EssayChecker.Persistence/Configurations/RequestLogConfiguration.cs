using EssayChecker.Domain.Entities.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class RequestLogConfiguration : IEntityTypeConfiguration<RequestLog>
{
    public void Configure(EntityTypeBuilder<RequestLog> builder)
    {
        builder.ToTable("RequestLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Method).HasMaxLength(10).IsRequired();
        builder.Property(l => l.Path).HasMaxLength(300).IsRequired();
        builder.Property(l => l.QueryString).HasMaxLength(2000);
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.CreatedAt).IsRequired();

        // Sütunlara görə axtarış üçün indekslər — istifadəçiyə, endpoint-ə, status koduna və
        // tarixə görə tez filtrləmə (tarixçəni sıralamaq üçün Id ilə birgə).
        builder.HasIndex(l => l.UserId);
        builder.HasIndex(l => l.Path);
        builder.HasIndex(l => l.StatusCode);
        builder.HasIndex(l => l.CreatedAt);
    }
}
