using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint24_5PortoesEPortas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CameraId",
                table: "PontosAcesso",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "EquipamentoAcionadorId",
                table: "PontosAcesso",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "NumeroSaida",
                table: "PontosAcesso",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AberturasPontoAcesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PontoAcessoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Sucesso = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mensagem = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OcorridoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AberturasPontoAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AberturasPontoAcesso_PontosAcesso_PontoAcessoId",
                        column: x => x.PontoAcessoId,
                        principalTable: "PontosAcesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PontosAcesso_CameraId",
                table: "PontosAcesso",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_PontosAcesso_EquipamentoAcionadorId",
                table: "PontosAcesso",
                column: "EquipamentoAcionadorId");

            migrationBuilder.CreateIndex(
                name: "IX_AberturasPontoAcesso_PontoAcessoId_OcorridoEmUtc",
                table: "AberturasPontoAcesso",
                columns: new[] { "PontoAcessoId", "OcorridoEmUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_PontosAcesso_Cameras_CameraId",
                table: "PontosAcesso",
                column: "CameraId",
                principalTable: "Cameras",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PontosAcesso_Equipamentos_EquipamentoAcionadorId",
                table: "PontosAcesso",
                column: "EquipamentoAcionadorId",
                principalTable: "Equipamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PontosAcesso_Cameras_CameraId",
                table: "PontosAcesso");

            migrationBuilder.DropForeignKey(
                name: "FK_PontosAcesso_Equipamentos_EquipamentoAcionadorId",
                table: "PontosAcesso");

            migrationBuilder.DropTable(
                name: "AberturasPontoAcesso");

            migrationBuilder.DropIndex(
                name: "IX_PontosAcesso_CameraId",
                table: "PontosAcesso");

            migrationBuilder.DropIndex(
                name: "IX_PontosAcesso_EquipamentoAcionadorId",
                table: "PontosAcesso");

            migrationBuilder.DropColumn(
                name: "CameraId",
                table: "PontosAcesso");

            migrationBuilder.DropColumn(
                name: "EquipamentoAcionadorId",
                table: "PontosAcesso");

            migrationBuilder.DropColumn(
                name: "NumeroSaida",
                table: "PontosAcesso");
        }
    }
}
