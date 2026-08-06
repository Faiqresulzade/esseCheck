using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EssayChecker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEssayScoreComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scores_ContentComment",
                table: "Essays",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Scores_GrammarComment",
                table: "Essays",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Scores_StructureComment",
                table: "Essays",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Scores_VocabularyComment",
                table: "Essays",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scores_ContentComment",
                table: "Essays");

            migrationBuilder.DropColumn(
                name: "Scores_GrammarComment",
                table: "Essays");

            migrationBuilder.DropColumn(
                name: "Scores_StructureComment",
                table: "Essays");

            migrationBuilder.DropColumn(
                name: "Scores_VocabularyComment",
                table: "Essays");
        }
    }
}
