using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneaGrab.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    RegistryId = table.Column<string>(type: "TEXT", nullable: false),
                    CallNumber = table.Column<string>(type: "TEXT", nullable: true),
                    FrameNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ArkUrl = table.Column<string>(type: "TEXT", nullable: true),
                    PageNumber = table.Column<string>(type: "TEXT", nullable: true),
                    SequenceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Position = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    Parish = table.Column<string>(type: "TEXT", nullable: true),
                    District = table.Column<string>(type: "TEXT", nullable: true),
                    Road = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    Sex = table.Column<int>(type: "INTEGER", nullable: true),
                    Age = table.Column<string>(type: "TEXT", nullable: true),
                    CivilStatus = table.Column<string>(type: "TEXT", nullable: true),
                    PlaceOrigin = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persons_Records_RecordId",
                        column: x => x.RecordId,
                        principalTable: "Records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Persons_RecordId",
                table: "Persons",
                column: "RecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Records");
        }
    }
}
