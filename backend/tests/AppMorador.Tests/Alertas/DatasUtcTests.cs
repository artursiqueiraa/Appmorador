using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppMorador.Tests.Alertas;

/// <summary>
/// Sprint 24 — datas "...Utc" lidas do banco precisam voltar marcadas como UTC, senão a API as
/// serializa sem "Z" e o celular mostra a hora 3 horas adiantada.
/// </summary>
public class DatasUtcTests
{
    [Fact]
    public async Task DataUtcLidaDoBanco_VoltaComKindUtc_EDataDeCalendarioNao()
    {
        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var id = Guid.NewGuid();
        var semFuso = new DateTime(2026, 11, 8, 19, 0, 0, DateTimeKind.Unspecified);

        await using (var db = new AppDbContext(opcoes))
        {
            db.AlertasCriticos.Add(new AlertaCritico
            {
                Id = id,
                PropriedadeId = Guid.NewGuid(),
                Tipo = TipoAlertaCritico.Panico,
                Descricao = "Teste",
                CriadoEmUtc = semFuso,
                ConfirmadoEmUtc = semFuso,
            });
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(opcoes))
        {
            var alerta = await db.AlertasCriticos.SingleAsync(a => a.Id == id);
            Assert.Equal(DateTimeKind.Utc, alerta.CriadoEmUtc.Kind);
            Assert.Equal(DateTimeKind.Utc, alerta.ConfirmadoEmUtc!.Value.Kind);
            Assert.Equal(semFuso.Ticks, alerta.CriadoEmUtc.Ticks);
        }

        var model = new AppDbContext(opcoes).Model;
        var dataInicial = model.FindEntityType(typeof(PermissaoAcesso))!.FindProperty(nameof(PermissaoAcesso.DataInicial))!;
        Assert.Null(dataInicial.GetValueConverter());
    }
}
