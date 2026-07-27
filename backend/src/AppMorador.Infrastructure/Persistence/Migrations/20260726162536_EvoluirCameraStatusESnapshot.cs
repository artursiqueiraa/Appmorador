using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EvoluirCameraStatusESnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Cameras",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaTentativaCapturaUtc",
                table: "Cameras",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoSnapshotPath",
                table: "Cameras",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoSucessoCapturaUtc",
                table: "Cameras",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "UltimaTentativaCapturaUtc",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "UltimoSnapshotPath",
                table: "Cameras");

            migrationBuilder.DropColumn(
                name: "UltimoSucessoCapturaUtc",
                table: "Cameras");
        }
    }
}
