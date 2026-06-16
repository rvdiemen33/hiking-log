#nullable disable

namespace HikingLog.Infrastructure.Migrations;

using System;
using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class InitialSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Routes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                TotalDistanceKm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Routes", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Stages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RouteId = table.Column<int>(type: "int", nullable: false),
                Number = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                StartPoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                EndPoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                DistanceKm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                ElevationDifferenceM = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: false),
                Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Stages", x => x.Id);
                table.ForeignKey(
                    name: "FK_Stages_Routes_RouteId",
                    column: x => x.RouteId,
                    principalTable: "Routes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "HikeLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StageId = table.Column<int>(type: "int", nullable: false),
                DateHiked = table.Column<DateOnly>(type: "date", nullable: false),
                DurationMinutes = table.Column<int>(type: "int", nullable: false),
                Weather = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                Rating = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HikeLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_HikeLogs_Stages_StageId",
                    column: x => x.StageId,
                    principalTable: "Stages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HikeLogs_StageId",
            table: "HikeLogs",
            column: "StageId");

        migrationBuilder.CreateIndex(
            name: "IX_Stages_RouteId",
            table: "Stages",
            column: "RouteId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HikeLogs");

        migrationBuilder.DropTable(
            name: "Stages");

        migrationBuilder.DropTable(
            name: "Routes");
    }
}
