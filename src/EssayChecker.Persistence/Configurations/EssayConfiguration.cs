using EssayChecker.Domain.Entities.Essays;
using EssayChecker.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class EssayConfiguration : IEntityTypeConfiguration<Essay>
{
    public void Configure(EntityTypeBuilder<Essay> builder)
    {
        builder.ToTable("Essays");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.OriginalText).IsRequired();
        builder.Property(e => e.CorrectedEssay).IsRequired();

        builder.Property(e => e.InputSource)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.UserId);

        // Statistika və ballar — sətir daxilində sütunlar (owned).
        builder.OwnsOne(e => e.Statistics);
        builder.OwnsOne(e => e.Scores);

        // Müəllim rəyi və səhvlər — JSON sütunlar (bütöv oxunub-yazılır).
        builder.OwnsOne(e => e.Feedback, f => f.ToJson());
        builder.OwnsMany(e => e.Mistakes, m => m.ToJson());
    }
}
