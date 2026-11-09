using AppMorador.Application.Alertas;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Alertas;
using AppMorador.Infrastructure.Persistence;
using AppMorador.Jfl.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Alertas;

public class VigiaConectividadeJflTests
{
    private const string NumeroSerie = "SN-JFL-1";

    private readonly string _banco = Guid.NewGuid().ToString();
    private readonly SessionManager _sessoes = new(NullLogger<SessionManager>.Instance);
    private readonly Mock<IAlertaCriticoServico> _alertas = new();
    private readonly VigiaConectividadeJfl _vigia;
    private readonly ServiceProvider _provider;
    private readonly Guid _equipamentoId = Guid.NewGuid();
    private readonly Guid _propriedadeId = Guid.NewGuid();

    public VigiaConectividadeJflTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_banco));
        services.AddScoped(_ => _alertas.Object);
        services.AddScoped(_ => new Mock<IOperacionalEventoPublicador>().Object);
        services.AddScoped(_ => new Mock<ISnapshotOperacionalServico>().Object);
        _provider = services.BuildServiceProvider();

        _vigia = new VigiaConectividadeJfl(
            _sessoes, new JflServerOptions { IntervaloKeepAliveMinutos = 5 },
            _provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<VigiaConectividadeJfl>.Instance);
    }

    private async Task CriarEquipamentoAsync(StatusEquipamento status, EstadoOperacionalEquipamento estado = EstadoOperacionalEquipamento.Ativo)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Equipamentos.Add(new Equipamento
        {
            Id = _equipamentoId,
            PropriedadeId = _propriedadeId,
            Nome = "Central da casa",
            Fabricante = FabricanteEquipamento.Jfl,
            Identificador = NumeroSerie,
            Status = status,
            EstadoOperacional = estado,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task<(Equipamento Equipamento, List<EventoConectividade> Eventos)> LerAsync()
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var equipamento = await db.Equipamentos.SingleAsync(e => e.Id == _equipamentoId);
        var eventos = await db.EventosConectividade.OrderBy(e => e.OcorridoEmUtc).ToListAsync();
        return (equipamento, eventos);
    }

    private void Conectar() =>
        _sessoes.Registrar(new JflSession(new MemoryStream(), "127.0.0.1:1234") { NumeroSerie = NumeroSerie });

    [Fact]
    public async Task CentralOnlineSemSessao_ViraOffline_RegistraPerdaEAbreAlerta()
    {
        await CriarEquipamentoAsync(StatusEquipamento.Online);

        await _vigia.VerificarAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        var (equipamento, eventos) = await LerAsync();
        Assert.Equal(StatusEquipamento.Offline, equipamento.Status);
        Assert.Equal(TipoEventoConectividade.Perdida, Assert.Single(eventos).Tipo);
        _alertas.Verify(a => a.CriarAsync(
            It.Is<NovoAlertaCritico>(n => n.Tipo == TipoAlertaCritico.SemComunicacao && n.EquipamentoId == _equipamentoId && n.PropriedadeId == _propriedadeId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SessaoAbertaMasSemKeepAliveHaMaisDe3Intervalos_ContaComoQuedaSilenciosa()
    {
        await CriarEquipamentoAsync(StatusEquipamento.Online);
        Conectar();

        // 5 min de keep-alive → limite de 15 min; 16 min depois sem atividade = queda.
        await _vigia.VerificarAsync(DateTimeOffset.UtcNow.AddMinutes(16), CancellationToken.None);

        var (equipamento, _) = await LerAsync();
        Assert.Equal(StatusEquipamento.Offline, equipamento.Status);
    }

    [Fact]
    public async Task SessaoComunicando_NaoFazNada()
    {
        await CriarEquipamentoAsync(StatusEquipamento.Online);
        Conectar();

        await _vigia.VerificarAsync(DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None);

        var (equipamento, eventos) = await LerAsync();
        Assert.Equal(StatusEquipamento.Online, equipamento.Status);
        Assert.Empty(eventos);
        _alertas.Verify(a => a.CriarAsync(It.IsAny<NovoAlertaCritico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VoltouAComunicarDepoisDaPerda_RegistraRetornoEParaDeEscalonar()
    {
        await CriarEquipamentoAsync(StatusEquipamento.Online);
        await _vigia.VerificarAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        Conectar();
        await _vigia.VerificarAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        var (equipamento, eventos) = await LerAsync();
        Assert.Equal(StatusEquipamento.Online, equipamento.Status);
        Assert.Equal([TipoEventoConectividade.Perdida, TipoEventoConectividade.Restabelecida], eventos.Select(e => e.Tipo));
        _alertas.Verify(a => a.EncerrarEscalonamentoAsync(_propriedadeId, TipoAlertaCritico.SemComunicacao, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CentralEmManutencao_NuncaAlerta()
    {
        await CriarEquipamentoAsync(StatusEquipamento.Online, EstadoOperacionalEquipamento.EmManutencao);

        await _vigia.VerificarAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        var (equipamento, eventos) = await LerAsync();
        Assert.Equal(StatusEquipamento.Online, equipamento.Status);
        Assert.Empty(eventos);
    }

    [Fact]
    public async Task CentralQueNuncaConectou_NaoGeraAlerta()
    {
        await CriarEquipamentoAsync(StatusEquipamento.Desconhecido);

        await _vigia.VerificarAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        var (_, eventos) = await LerAsync();
        Assert.Empty(eventos);
        _alertas.Verify(a => a.CriarAsync(It.IsAny<NovoAlertaCritico>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
