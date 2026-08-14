using EssayChecker.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class EnglishTeacherConfiguration : IEntityTypeConfiguration<EnglishTeacher>
{
    public void Configure(EntityTypeBuilder<EnglishTeacher> builder)
    {
        builder.ToTable("EnglishTeachers");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.FullName).HasMaxLength(200).IsRequired();
        builder.Property(t => t.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(t => t.City).HasMaxLength(100);
        builder.Property(t => t.ProfileUrl).HasMaxLength(500);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.IsContacted).HasDefaultValue(false);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.PhoneNumber);
        builder.HasIndex(t => t.IsContacted);
    }
}
