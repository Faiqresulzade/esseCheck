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

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Şagird bazadan tam silinsə dərs itməsin — esselərdəki eyni prinsip.
        builder.HasOne<Domain.Entities.Teaching.Student>()
            .WithMany()
            .HasForeignKey(l => l.StudentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.UserId);

        builder.HasIndex(l => l.StudentId)
            .HasFilter("\"StudentId\" IS NOT NULL");

        // "Bu istifadəçi bu mövzunu artıq soruşub?" sorğusu — təkrar yaradılışın qarşısını alır.
        builder.HasIndex(l => new { l.UserId, l.NormalizedTopic, l.Grade });

        // Slayd və test məzmunu JSON sütunlardır: bütöv oxunub-yazılır, daxili sahələrə görə
        // heç vaxt sorğu getmir (esse Mistakes/Feedback ilə eyni yanaşma).
        builder.OwnsMany(l => l.Slides, ConfigureSlides);
        builder.OwnsMany(l => l.Quiz, ConfigureQuiz);
    }

    internal static void ConfigureSlides<T>(OwnedNavigationBuilder<T, LessonSlide> slides) where T : class
    {
        slides.ToJson();
        slides.Property(s => s.Type).HasConversion<string>();
        slides.OwnsMany(s => s.Examples);
        slides.OwnsMany(s => s.Mistakes);
        slides.OwnsOne(s => s.Comparison);
    }

    internal static void ConfigureQuiz<T>(OwnedNavigationBuilder<T, LessonQuizQuestion> quiz) where T : class
    {
        quiz.ToJson();
    }
}
