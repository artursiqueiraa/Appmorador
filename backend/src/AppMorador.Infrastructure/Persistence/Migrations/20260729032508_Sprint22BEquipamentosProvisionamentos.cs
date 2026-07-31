using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppMorador.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint22BEquipamentosProvisionamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Identificador",
                table: "Equipamentos",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EstadoOperacional",
                table: "Equipamentos",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "Equipamentos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Equipamentos",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VinculosEquipamentoPropriedade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EquipamentoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PropriedadeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DataInicioUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataFimUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CriadoPorUsuarioId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Observacoes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosEquipamentoPropriedade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VinculosEquipamentoPropriedade_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VinculosEquipamentoPropriedade_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_PropriedadeId_Identificador",
                table: "Equipamentos",
                columns: new[] { "PropriedadeId", "Identificador" },
                unique: true);

            // A FK de Equipamentos->Propriedades depende de um índice com PropriedadeId como
            // coluna líder — o índice composto acima já cobre essa necessidade, então só agora
            // (depois de criado) é seguro remover o índice simples antigo.
            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_PropriedadeId",
                table: "Equipamentos");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosEquipamentoPropriedade_EquipamentoId",
                table: "VinculosEquipamentoPropriedade",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosEquipamentoPropriedade_PropriedadeId",
                table: "VinculosEquipamentoPropriedade",
                column: "PropriedadeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VinculosEquipamentoPropriedade");

            migrationBuilder.DropColumn(
                name: "EstadoOperacional",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "Equipamentos");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Equipamentos");

            migrationBuilder.AlterColumn<string>(
                name: "Identificador",
                table: "Equipamentos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // Mesmo motivo do Up(): criar o índice simples antes de derrubar o composto,
            // para nunca deixar a FK Equipamentos->Propriedades sem índice de apoio.
            migrationBuilder.CreateIndex(
                name: "IX_Equipamentos_PropriedadeId",
                table: "Equipamentos",
                column: "PropriedadeId");

            migrationBuilder.DropIndex(
                name: "IX_Equipamentos_PropriedadeId_Identificador",
                table: "Equipamentos");
        }
    }
}
