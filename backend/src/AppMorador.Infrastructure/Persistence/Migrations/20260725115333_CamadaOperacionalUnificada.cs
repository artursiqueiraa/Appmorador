using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CamadaOperacionalUnificada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SnapshotsOperacionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GeradoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Saude = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuantidadeEquipamentosOnline = table.Column<int>(type: "int", nullable: false),
                    QuantidadeEquipamentosOffline = table.Column<int>(type: "int", nullable: false),
                    UltimaComunicacaoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    QuantidadeEventosHoje = table.Column<int>(type: "int", nullable: false),
                    QuantidadeAlarmesAtivos = table.Column<int>(type: "int", nullable: false),
                    QuantidadeFalhasDetectadas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotsOperacionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SnapshotsOperacionais_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotsOperacionais_PropriedadeId",
                table: "SnapshotsOperacionais",
                column: "PropriedadeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SnapshotsOperacionais");
        }
    }
}
