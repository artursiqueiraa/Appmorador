using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarVisitantesEAutorizacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Visitantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Documento = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefone = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FotoPath = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Observacoes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataExclusaoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExcluidoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visitantes_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Autorizacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MoradorResponsavelId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UnidadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VisitanteId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataInicial = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataFinal = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HorarioInicial = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    HorarioFinal = table.Column<TimeOnly>(type: "time(6)", nullable: true),
                    StatusManual = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataExclusaoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExcluidoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autorizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Moradores_MoradorResponsavelId",
                        column: x => x.MoradorResponsavelId,
                        principalTable: "Moradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Unidades_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Autorizacoes_Visitantes_VisitanteId",
                        column: x => x.VisitanteId,
                        principalTable: "Visitantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistoricoVisitantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VisitanteId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AutorizacaoId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TipoEvento = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoVisitantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoVisitantes_Autorizacoes_AutorizacaoId",
                        column: x => x.AutorizacaoId,
                        principalTable: "Autorizacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HistoricoVisitantes_Visitantes_VisitanteId",
                        column: x => x.VisitanteId,
                        principalTable: "Visitantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_MoradorResponsavelId",
                table: "Autorizacoes",
                column: "MoradorResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_UnidadeId",
                table: "Autorizacoes",
                column: "UnidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Autorizacoes_VisitanteId",
                table: "Autorizacoes",
                column: "VisitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoVisitantes_AutorizacaoId",
                table: "HistoricoVisitantes",
                column: "AutorizacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoVisitantes_VisitanteId",
                table: "HistoricoVisitantes",
                column: "VisitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitantes_PropriedadeId",
                table: "Visitantes",
                column: "PropriedadeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoricoVisitantes");

            migrationBuilder.DropTable(
                name: "Autorizacoes");

            migrationBuilder.DropTable(
                name: "Visitantes");
        }
    }
}
