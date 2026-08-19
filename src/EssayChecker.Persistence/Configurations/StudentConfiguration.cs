using EssayChecker.Domain.Entities.Teaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.FullName)
            .HasMaxLength(100)
            .IsRequired();

        // Essay.Grade ilə eyni konvensiya — bazada oxunaqlı string kimi saxlanılır.
        builder.Property(s => s.Grade)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.CreatedAt).IsRequired();

        builder.HasOne(s => s.Group)
            .WithMany(g => g.Students)
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.GroupId)
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
