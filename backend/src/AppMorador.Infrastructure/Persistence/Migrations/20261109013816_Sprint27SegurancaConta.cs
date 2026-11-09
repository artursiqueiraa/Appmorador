using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint27SegurancaConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PinBloqueadoAteUtc",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinConfirmacaoHash",
                table: "Usuarios",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TentativasPinFalhas",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ComandosExecutados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EquipamentoId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PontoAcessoId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Parametros = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confirmacao = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sucesso = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mensagem = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChaveIdempotencia = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RespostaJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Concluido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OcorridoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComandosExecutados", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RegistrosBackup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sucesso = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Arquivo = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: true),
                    Mensagem = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OcorridoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosBackup", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ComandosExecutados_PropriedadeId_OcorridoEmUtc",
                table: "ComandosExecutados",
                columns: new[] { "PropriedadeId", "OcorridoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ComandosExecutados_UsuarioId_ChaveIdempotencia",
                table: "ComandosExecutados",
                columns: new[] { "UsuarioId", "ChaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosBackup_Tipo_OcorridoEmUtc",
                table: "RegistrosBackup",
                columns: new[] { "Tipo", "OcorridoEmUtc" });

            // Histórico de aberturas da Sprint 24.5 copiado para a auditoria única (a tabela antiga fica
            // como está, sem novos registros). Mesmo Id: rodar de novo não duplica.
            migrationBuilder.Sql(@"
INSERT INTO ComandosExecutados (Id, PropriedadeId, UsuarioId, EquipamentoId, PontoAcessoId, Tipo, Parametros, Confirmacao, Sucesso, Mensagem, ChaveIdempotencia, RespostaJson, Concluido, OcorridoEmUtc)
SELECT a.Id, p.PropriedadeId, a.UsuarioId, p.EquipamentoAcionadorId, a.PontoAcessoId, 'AbrirAcesso', NULL, 'Nenhuma', a.Sucesso, a.Mensagem, NULL, NULL, 1, a.OcorridoEmUtc
FROM AberturasPontoAcesso a
JOIN PontosAcesso p ON p.Id = a.PontoAcessoId
WHERE NOT EXISTS (SELECT 1 FROM ComandosExecutados c WHERE c.Id = a.Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComandosExecutados");

            migrationBuilder.DropTable(
                name: "RegistrosBackup");

            migrationBuilder.DropColumn(
                name: "PinBloqueadoAteUtc",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PinConfirmacaoHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TentativasPinFalhas",
                table: "Usuarios");
        }
    }
}
