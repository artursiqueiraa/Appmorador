using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint26FacialLgpd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsentimentosBiometricos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MoradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RegistradoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VersaoTermo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponsavelLegal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RegistradoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevogadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentimentosBiometricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentimentosBiometricos_Moradores_MoradorId",
                        column: x => x.MoradorId,
                        principalTable: "Moradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SincronizacoesFoto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MoradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EquipamentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Situacao = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Operacao = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mensagem = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SincronizacoesFoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SincronizacoesFoto_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentimentosBiometricos_MoradorId_RevogadoEmUtc",
                table: "ConsentimentosBiometricos",
                columns: new[] { "MoradorId", "RevogadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SincronizacoesFoto_EquipamentoId",
                table: "SincronizacoesFoto",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SincronizacoesFoto_MoradorId_EquipamentoId",
                table: "SincronizacoesFoto",
                columns: new[] { "MoradorId", "EquipamentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SincronizacoesFoto_Situacao",
                table: "SincronizacoesFoto",
                column: "Situacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentimentosBiometricos");

            migrationBuilder.DropTable(
                name: "SincronizacoesFoto");
        }
    }
}
