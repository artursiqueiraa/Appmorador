using AppMorador.Application.Notificacoes;
using AppMorador.Infrastructure.Notifications;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AppMorador.Tests.Notificacoes;

/// <summary>Sprint 24 (ADR 0040) — substitui os testes do debounce em memória (dívida 41).</summary>
public class DebounceNotificacaoPersistidoTests
{
    private static DbContextOptions<AppDbContext> NovoBanco() =>
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    [Fact]
    public async Task PrimeiroEnvio_PodeNotificar()
    {
        await using var db = new AppDbContext(NovoBanco());
        var debounce = new DebounceNotificacaoPersistido(db);

        Assert.True(await debounce.PodeNotificarAsync(EventoNotificacaoTipo.AlarmeDisparado, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task LogoAposRegistrar_Suprime()
    {
        await using var db = new AppDbContext(NovoBanco());
        var debounce = new DebounceNotificacaoPersistido(db);
        var chave = Guid.NewGuid();

        await debounce.RegistrarEnvioAsync(EventoNotificacaoTipo.AlarmeDisparado, chave, CancellationToken.None);

        Assert.False(await debounce.PodeNotificarAsync(EventoNotificacaoTipo.AlarmeDisparado, chave, CancellationToken.None));
    }

    [Fact]
    public async Task OutroTipoOuOutraChave_NaoSaoSuprimidos()
    {
        await using var db = new AppDbContext(NovoBanco());
        var debounce = new DebounceNotificacaoPersistido(db);
        var chave = Guid.NewGuid();

        await debounce.RegistrarEnvioAsync(EventoNotificacaoTipo.AlarmeDisparado, chave, CancellationToken.None);

        Assert.True(await debounce.PodeNotificarAsync(EventoNotificacaoTipo.SistemaArmado, chave, CancellationToken.None));
        Assert.True(await debounce.PodeNotificarAsync(EventoNotificacaoTipo.AlarmeDisparado, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task SobreviveAoReinicio_NovoContextoNoMesmoBancoContinuaSuprimindo()
    {
        var opcoes = NovoBanco();
        var chave = Guid.NewGuid();

        await using (var db = new AppDbContext(opcoes))
        {
            await new DebounceNotificacaoPersistido(db).RegistrarEnvioAsync(EventoNotificacaoTipo.EquipamentoOffline, chave, CancellationToken.None);
        }

        await using (var db = new AppDbContext(opcoes))
        {
            Assert.False(await new DebounceNotificacaoPersistido(db).PodeNotificarAsync(EventoNotificacaoTipo.EquipamentoOffline, chave, CancellationToken.None));
        }
    }

    [Fact]
    public async Task DepoisDaJanela_PodeNotificarDeNovo()
    {
        await using var db = new AppDbContext(NovoBanco());
        var debounce = new DebounceNotificacaoPersistido(db);
        var chave = Guid.NewGuid();
        await debounce.RegistrarEnvioAsync(EventoNotificacaoTipo.AlarmeDisparado, chave, CancellationToken.None);

        var controle = await db.ControlesEnvioNotificacao.SingleAsync();
        controle.UltimoEnvioEmUtc = DateTime.UtcNow - DebounceNotificacaoPersistido.Janela - TimeSpan.FromSeconds(1);
        await db.SaveChangesAsync();

        Assert.True(await debounce.PodeNotificarAsync(EventoNotificacaoTipo.AlarmeDisparado, chave, CancellationToken.None));
    }
}
