using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EssayChecker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceTrials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceTrials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceIdHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GrantedToUserId = table.Column<int>(type: "integer", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceTrials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceTrials_DeviceIdHash",
                table: "DeviceTrials",
                column: "DeviceIdHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceTrials");
        }
    }
}
