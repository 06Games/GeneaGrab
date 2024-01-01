using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneaGrab.Migrations
{
    /// <inheritdoc />
    public partial class ExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Extra",
                table: "Registries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Extra",
                table: "Frames",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Extra",
                table: "Registries");

            migrationBuilder.DropColumn(
                name: "Extra",
                table: "Frames");
        }
    }
}
