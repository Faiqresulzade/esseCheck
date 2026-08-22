using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EssayChecker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReshapeLessonsIntoSharedLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_AspNetUsers_UserId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Students_StudentId",
                table: "Lessons");

            migrationBuilder.DropTable(
                name: "LessonTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_StudentId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_UserId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_UserId_NormalizedTopic_Grade",
                table: "Lessons");

            // Auto-generated köçürmə "UserId"-i sadəcə "CreatedByUserId"-ə YENİDƏN ADLANDIRARDI —
            // amma o zaman köhnə dəyər itməzdi. Əl ilə əlavə addım kimi yeni sütun yaradıb köhnə
            // dəyəri KOPYALAYIRIQ, sonra köhnə sütunları siliriq. Bax: canlı bazada bu köçürmədən
            // əvvəl 4 sətir var idi, hamısının UserId=13 idi — bu addım olmasaydı defaultValue=0
            // yazılardı və aşağıdakı FK əlavəsi AspNetUsers-də Id=0 tapılmadığı üçün UĞURSUZ olardı.
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Lessons\" SET \"CreatedByUserId\" = \"UserId\";");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lessons");

            // Mövcud sətirlərin hansı prompt versiyası ilə yaradıldığı bilinmir (sahə bu
            // köçürmədən əvvəl yox idi) — 1 defolt dəyər kimi qoyulur. Bu sahə avtomatik
            // köhnəlməni idarə etmir (bax Lesson.PromptVersion şərhi), ona görə yanlış dəyər
            // funksional nəticə doğurmur, yalnız məlumat xarakterlidir.
            migrationBuilder.AddColumn<int>(
                name: "PromptVersion",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CreatedByUserId",
                table: "Lessons",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_NormalizedTopic_Grade",
                table: "Lessons",
                columns: new[] { "NormalizedTopic", "Grade" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedByUserId",
                table: "Lessons",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedByUserId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_CreatedByUserId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_NormalizedTopic_Grade",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "PromptVersion",
                table: "Lessons");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Lessons\" SET \"UserId\" = \"CreatedByUserId\";");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Lessons");

            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "Lessons",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LessonTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NormalizedTopic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PromptVersion = table.Column<int>(type: "integer", nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quiz = table.Column<string>(type: "jsonb", nullable: true),
                    Slides = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_StudentId",
                table: "Lessons",
                column: "StudentId",
                filter: "\"StudentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UserId",
                table: "Lessons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UserId_NormalizedTopic_Grade",
                table: "Lessons",
                columns: new[] { "UserId", "NormalizedTopic", "Grade" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonTemplates_NormalizedTopic_Grade_PromptVersion",
                table: "LessonTemplates",
                columns: new[] { "NormalizedTopic", "Grade", "PromptVersion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_AspNetUsers_UserId",
                table: "Lessons",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Students_StudentId",
                table: "Lessons",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
