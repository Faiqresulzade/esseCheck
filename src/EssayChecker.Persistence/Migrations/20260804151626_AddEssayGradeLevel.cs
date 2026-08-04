using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EssayChecker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEssayGradeLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Köhnə (sinif seçimindən əvvəlki) qeydlər üçün defolt dəyər lazımdır — "" etibarsızdır,
            // çünki Grade sütunu string-ə çevrilmiş enum-dur (Grade9/Grade11), boş sətir isə heç
            // birinə uyğun gəlmir və oxunduqda deserializasiya xətası yaradar.
            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "Essays",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Grade11");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Essays");
        }
    }
}
