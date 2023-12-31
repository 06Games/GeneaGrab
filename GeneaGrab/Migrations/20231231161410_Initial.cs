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
                name: "Registries",
                columns: table => new
                {
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CallNumber = table.Column<string>(type: "TEXT", nullable: true),
                    URL = table.Column<string>(type: "TEXT", nullable: true),
                    ArkURL = table.Column<string>(type: "TEXT", nullable: true),
                    Types = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registries", x => new { x.ProviderId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "Frames",
                columns: table => new
                {
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    RegistryId = table.Column<string>(type: "TEXT", nullable: false),
                    FrameNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ArkUrl = table.Column<string>(type: "TEXT", nullable: true),
                    DownloadUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ImageSize = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    TileSize = table.Column<int>(type: "INTEGER", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Frames", x => new { x.ProviderId, x.RegistryId, x.FrameNumber });
                    table.ForeignKey(
                        name: "FK_Frames_Registries_ProviderId_RegistryId",
                        columns: x => new { x.ProviderId, x.RegistryId },
                        principalTable: "Registries",
                        principalColumns: new[] { "ProviderId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Records",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProviderId = table.Column<string>(type: "TEXT", nullable: false),
                    RegistryId = table.Column<string>(type: "TEXT", nullable: false),
                    RegistryProviderId = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Records_Frames_ProviderId_RegistryId_FrameNumber",
                        columns: x => new { x.ProviderId, x.RegistryId, x.FrameNumber },
                        principalTable: "Frames",
                        principalColumns: new[] { "ProviderId", "RegistryId", "FrameNumber" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Records_Registries_RegistryProviderId_RegistryId",
                        columns: x => new { x.RegistryProviderId, x.RegistryId },
                        principalTable: "Registries",
                        principalColumns: new[] { "ProviderId", "Id" });
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
                    Relation = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Records_ProviderId_RegistryId_FrameNumber",
                table: "Records",
                columns: new[] { "ProviderId", "RegistryId", "FrameNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Records_RegistryProviderId_RegistryId",
                table: "Records",
                columns: new[] { "RegistryProviderId", "RegistryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropTable(
                name: "Records");

            migrationBuilder.DropTable(
                name: "Frames");

            migrationBuilder.DropTable(
                name: "Registries");
        }
    }
}
