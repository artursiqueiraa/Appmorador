using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint24AlertasConfiaveis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinutosEscalonamento",
                table: "Propriedades",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "AlertasCriticos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OcorrenciaId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    EquipamentoId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    AcionadoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConfirmadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConfirmadoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    NivelEscalonamento = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProximoEscalonamentoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EscalonadoMembrosEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EscalonadoContatosEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasCriticos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasCriticos_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertasCriticos_Usuarios_ConfirmadoPorUsuarioId",
                        column: x => x.ConfirmadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContatosEmergencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Excluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataExclusaoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExcluidoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContatosEmergencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContatosEmergencia_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ControlesEnvioNotificacao",
                columns: table => new
                {
                    Chave = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UltimoEnvioEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlesEnvioNotificacao", x => x.Chave);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EventosConectividade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EquipamentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OcorridoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosConectividade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosConectividade_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCriticos_ConfirmadoPorUsuarioId",
                table: "AlertasCriticos",
                column: "ConfirmadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCriticos_PropriedadeId_CriadoEmUtc",
                table: "AlertasCriticos",
                columns: new[] { "PropriedadeId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCriticos_ProximoEscalonamentoEmUtc",
                table: "AlertasCriticos",
                column: "ProximoEscalonamentoEmUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ContatosEmergencia_PropriedadeId_Ordem",
                table: "ContatosEmergencia",
                columns: new[] { "PropriedadeId", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosConectividade_EquipamentoId_OcorridoEmUtc",
                table: "EventosConectividade",
                columns: new[] { "EquipamentoId", "OcorridoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosConectividade_PropriedadeId_OcorridoEmUtc",
                table: "EventosConectividade",
                columns: new[] { "PropriedadeId", "OcorridoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasCriticos");

            migrationBuilder.DropTable(
                name: "ContatosEmergencia");

            migrationBuilder.DropTable(
                name: "ControlesEnvioNotificacao");

            migrationBuilder.DropTable(
                name: "EventosConectividade");

            migrationBuilder.DropColumn(
                name: "MinutosEscalonamento",
                table: "Propriedades");
        }
    }
}
