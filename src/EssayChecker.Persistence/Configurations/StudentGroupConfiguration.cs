using EssayChecker.Domain.Entities.Teaching;
using EssayChecker.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.ToTable("StudentGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.CreatedAt).IsRequired();

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(g => g.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Əsas sorğu: "bu müəllimin silinməmiş qrupları" — filtrli index tam onu qarşılayır.
        builder.HasIndex(g => g.TeacherId)
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
