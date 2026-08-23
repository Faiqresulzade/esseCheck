using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EssayChecker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeLessonCreatorNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedByUserId",
                table: "Lessons");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "Lessons",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedByUserId",
                table: "Lessons",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedByUserId",
                table: "Lessons");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedByUserId",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_AspNetUsers_CreatedByUserId",
                table: "Lessons",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
