using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RbacMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Sprint 21 (ADR 0021) — o default de coluna acima (false) só existe pra
            // satisfazer o NOT NULL; sem este backfill, toda conta CRIADA ANTES desta
            // Sprint ficaria com Ativo=false (login bloqueado). Só contas novas devem
            // nascer com o valor default do model (true, ver Usuario.cs).
            migrationBuilder.Sql("UPDATE Usuarios SET Ativo = 1;");

            migrationBuilder.AddColumn<string>(
                name: "RoleGlobal",
                table: "Usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "ModeloEquipamentoId",
                table: "Equipamentos",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "AuditoriaMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioNome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Acao = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Entidade = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntidadeId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Detalhes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataHoraUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaMaster", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModelosEquipamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Fabricante = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosEquipamento", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PropriedadesFeatureFlag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Feature = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AtivadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropriedadesFeatureFlag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropriedadesFeatureFlag_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Provisionamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Template = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provisionamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Provisionamentos_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UsuariosPropriedade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Perfil = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosPropriedade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosPropriedade_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosPropriedade_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ModelosEquipamentoCapacidade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ModeloEquipamentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Capacidade = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelosEquipamentoCapacidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelosEquipamentoCapacidade_ModelosEquipamento_ModeloEquipa~",
                        column: x => x.ModeloEquipamentoId,
                        principalTable: "ModelosEquipamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UsuariosPropriedadePermissao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsuarioPropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Permissao = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosPropriedadePermissao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosPropriedadePermissao_UsuariosPropriedade_UsuarioProp~",
                        column: x => x.UsuarioPropriedadeId,
                        principalTable: "UsuariosPropriedade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_ModeloEquipamentoId",
                table: "Equipamentos",
                column: "ModeloEquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaMaster_DataHoraUtc",
                table: "AuditoriaMaster",
                column: "DataHoraUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaMaster_UsuarioId",
                table: "AuditoriaMaster",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelosEquipamento_Fabricante_Nome",
                table: "ModelosEquipamento",
                columns: new[] { "Fabricante", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelosEquipamentoCapacidade_ModeloEquipamentoId_Capacidade",
                table: "ModelosEquipamentoCapacidade",
                columns: new[] { "ModeloEquipamentoId", "Capacidade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PropriedadesFeatureFlag_PropriedadeId_Feature",
                table: "PropriedadesFeatureFlag",
                columns: new[] { "PropriedadeId", "Feature" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Provisionamentos_PropriedadeId",
                table: "Provisionamentos",
                column: "PropriedadeId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPropriedade_PropriedadeId",
                table: "UsuariosPropriedade",
                column: "PropriedadeId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPropriedade_UsuarioId_PropriedadeId",
                table: "UsuariosPropriedade",
                columns: new[] { "UsuarioId", "PropriedadeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPropriedadePermissao_UsuarioPropriedadeId_Permissao",
                table: "UsuariosPropriedadePermissao",
                columns: new[] { "UsuarioPropriedadeId", "Permissao" },
                unique: true);

            // Sprint 21 (ADR 0021/0027) — backfill: Equipamento.Modelo (texto livre) vira
            // ModeloEquipamento (catalogo deduplicado por Fabricante+Nome) ANTES da coluna
            // de texto ser derrubada, preservando os dados existentes (nenhum "Modelo"
            // hoje cadastrado se perde — ele passa a existir como linha em
            // ModelosEquipamento e o Equipamento aponta pra ela via ModeloEquipamentoId).
            migrationBuilder.Sql(
                @"INSERT INTO ModelosEquipamento (Id, Fabricante, Nome, CreatedAtUtc)
                  SELECT UUID(), t.Fabricante, t.Modelo, UTC_TIMESTAMP(6)
                  FROM (
                      SELECT DISTINCT Fabricante, Modelo
                      FROM Equipamentos
                      WHERE Modelo IS NOT NULL AND TRIM(Modelo) <> ''
                  ) AS t;");

            migrationBuilder.Sql(
                @"UPDATE Equipamentos e
                  JOIN ModelosEquipamento m ON e.Fabricante = m.Fabricante AND e.Modelo = m.Nome
                  SET e.ModeloEquipamentoId = m.Id
                  WHERE e.Modelo IS NOT NULL AND TRIM(e.Modelo) <> '';");

            migrationBuilder.DropColumn(
                name: "Modelo",
                table: "Equipamentos");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipamentos_ModelosEquipamento_ModeloEquipamentoId",
                table: "Equipamentos",
                column: "ModeloEquipamentoId",
                principalTable: "ModelosEquipamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipamentos_ModelosEquipamento_ModeloEquipamentoId",
                table: "Equipamentos");

            migrationBuilder.DropTable(
                name: "AuditoriaMaster");

            migrationBuilder.DropTable(
                name: "ModelosEquipamentoCapacidade");

            migrationBuilder.DropTable(
                name: "PropriedadesFeatureFlag");

            migrationBuilder.DropTable(
                name: "Provisionamentos");

            migrationBuilder.DropTable(
                name: "UsuariosPropriedadePermissao");

            migrationBuilder.DropTable(
                name: "ModelosEquipamento");

            migrationBuilder.DropTable(
                name: "UsuariosPropriedade");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_ModeloEquipamentoId",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "RoleGlobal",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ModeloEquipamentoId",
                table: "Equipamentos");

            migrationBuilder.AddColumn<string>(
                name: "Modelo",
                table: "Equipamentos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
