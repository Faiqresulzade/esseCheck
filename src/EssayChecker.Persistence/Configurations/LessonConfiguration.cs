using EssayChecker.Domain.Entities.Lessons;
using EssayChecker.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Topic)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.NormalizedTopic)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Grade)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(l => l.CreatedAt).IsRequired();

        // Yaradan hesab silinsə dərs kitabxanada QALMALIDIR — o, artıq ortaq resursdur və başqa
        // müəllimlər ondan istifadə edir. Ona görə Cascade deyil, Restrict.
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(l => l.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.CreatedByUserId);

        // Kitabxananın əsas qaydası: bir mövzu+sinif = bir dərs. Təkrar yaradılış həm token
        // israfıdır, həm də siyahıda dublikat göstərər.
        builder.HasIndex(l => new { l.NormalizedTopic, l.Grade })
            .IsUnique();

        // Slayd və test məzmunu JSON sütunlardır: bütöv oxunub-yazılır, daxili sahələrə görə
        // heç vaxt sorğu getmir (esse Mistakes/Feedback ilə eyni yanaşma).
        builder.OwnsMany(l => l.Slides, slides =>
        {
            slides.ToJson();
            slides.Property(s => s.Type).HasConversion<string>();
            slides.OwnsMany(s => s.Examples);
            slides.OwnsMany(s => s.Mistakes);
            slides.OwnsOne(s => s.Comparison);
        });

        builder.OwnsMany(l => l.Quiz, quiz => quiz.ToJson());
    }
}
