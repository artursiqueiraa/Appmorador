using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarControleDeAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Credenciais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MoradorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataExclusaoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExcluidoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credenciais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Credenciais_Moradores_MoradorId",
                        column: x => x.MoradorId,
                        principalTable: "Moradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PontosAcesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataExclusaoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExcluidoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PontosAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PontosAcesso_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistoricoCredenciais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CredencialId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TipoEvento = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoCredenciais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoCredenciais_Credenciais_CredencialId",
                        column: x => x.CredencialId,
                        principalTable: "Credenciais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PermissoesAcesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CredencialId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PontoAcessoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DiasPermitidos = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HorarioInicial = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    HorarioFinal = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    DataInicial = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataFinal = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataExclusaoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExcluidoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissoesAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissoesAcesso_Credenciais_CredencialId",
                        column: x => x.CredencialId,
                        principalTable: "Credenciais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissoesAcesso_PontosAcesso_PontoAcessoId",
                        column: x => x.PontoAcessoId,
                        principalTable: "PontosAcesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Credenciais_MoradorId",
                table: "Credenciais",
                column: "MoradorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoCredenciais_CredencialId",
                table: "HistoricoCredenciais",
                column: "CredencialId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissoesAcesso_CredencialId",
                table: "PermissoesAcesso",
                column: "CredencialId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissoesAcesso_PontoAcessoId",
                table: "PermissoesAcesso",
                column: "PontoAcessoId");

            migrationBuilder.CreateIndex(
                name: "IX_PontosAcesso_PropriedadeId",
                table: "PontosAcesso",
                column: "PropriedadeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoCredenciais");

            migrationBuilder.DropTable(
                name: "PermissoesAcesso");

            migrationBuilder.DropTable(
                name: "Credenciais");

            migrationBuilder.DropTable(
                name: "PontosAcesso");
        }
    }
}
