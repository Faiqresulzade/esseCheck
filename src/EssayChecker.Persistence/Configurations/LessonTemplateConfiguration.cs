using EssayChecker.Domain.Entities.Lessons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EssayChecker.Persistence.Configurations;

public class LessonTemplateConfiguration : IEntityTypeConfiguration<LessonTemplate>
{
    public void Configure(EntityTypeBuilder<LessonTemplate> builder)
    {
        builder.ToTable("LessonTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Topic)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.NormalizedTopic)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Grade)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.CreatedAt).IsRequired();

        // Keşin açarı. PromptVersion açarın içindədir: prompt dəyişəndə köhnə sətirlər sadəcə
        // bir daha tapılmır, silmək lazım gəlmir.
        builder.HasIndex(t => new { t.NormalizedTopic, t.Grade, t.PromptVersion })
            .IsUnique();

        builder.OwnsMany(t => t.Slides, LessonConfiguration.ConfigureSlides);
        builder.OwnsMany(t => t.Quiz, LessonConfiguration.ConfigureQuiz);
    }
}
