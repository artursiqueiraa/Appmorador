using System.Text.Json;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using AppMorador.Infrastructure.Jfl;
using AppMorador.Jfl.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Jfl;

public class EquipamentoJflConexaoObserverTests
{
    private static JflSession NovaSessao(string numeroSerie, byte? modelo = null, string? firmware = null, string? mac = null)
    {
        var session = new JflSession(new MemoryStream(), "127.0.0.1:1234")
        {
            NumeroSerie = numeroSerie,
            Modelo = modelo,
            VersaoFirmware = firmware,
            Mac = mac,
        };
        return session;
    }

    private static (Mock<IEquipamentoRepositorio> Equipamentos, IServiceProvider Provider, EquipamentoJflConexaoObserver Observer) NovoObservador()
    {
        var equipamentos = new Mock<IEquipamentoRepositorio>();

        var services = new ServiceCollection();
        services.AddScoped(_ => equipamentos.Object);
        var provider = services.BuildServiceProvider();

        var sessionManager = new SessionManager(NullLogger<SessionManager>.Instance);
        var observer = new EquipamentoJflConexaoObserver(sessionManager, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<EquipamentoJflConexaoObserver>.Instance);

        return (equipamentos, provider, observer);
    }

    [Fact]
    public async Task ProcessarAsync_NenhumEquipamentoCorrespondente_NaoLancaENaoSalva()
    {
        var (equipamentos, _, observer) = NovoObservador();
        equipamentos
            .Setup(e => e.GetByFabricanteEIdentificadorAsync(FabricanteEquipamento.Jfl, "SN-999", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Equipamento?)null);

        await observer.ProcessarAsync(NovaSessao("SN-999"));

        equipamentos.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessarAsync_EquipamentoEncontrado_MarcaOnlineEPersisteDescobertasReais()
    {
        var (equipamentos, _, observer) = NovoObservador();
        var equipamento = new Equipamento
        {
            Id = Guid.NewGuid(),
            PropriedadeId = Guid.NewGuid(),
            Nome = "Central JFL",
            Fabricante = FabricanteEquipamento.Jfl,
            Identificador = "SN-123",
            Status = StatusEquipamento.Desconhecido,
            CreatedAtUtc = DateTime.UtcNow,
        };
        equipamentos
            .Setup(e => e.GetByFabricanteEIdentificadorAsync(FabricanteEquipamento.Jfl, "SN-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipamento);

        await observer.ProcessarAsync(NovaSessao("SN-123", modelo: 0xA4, firmware: "1.02", mac: "AA:BB:CC:DD:EE:FF"));

        Assert.Equal(StatusEquipamento.Online, equipamento.Status);
        Assert.Equal("AA:BB:CC:DD:EE:FF", equipamento.MacAddress);
        Assert.NotNull(equipamento.UltimaSincronizacaoUtc);
        Assert.NotNull(equipamento.UltimaDescobertaUtc);

        var descobertas = JsonSerializer.Deserialize<Dictionary<string, string>>(equipamento.InformacoesDescobertasJson!)!;
        Assert.Equal("Active 100 Bus", descobertas["Modelo"]);
        Assert.Equal("1.02", descobertas["Firmware"]);

        equipamentos.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessarAsync_RepositorioLancaExcecao_NuncaPropaga()
    {
        var (equipamentos, _, observer) = NovoObservador();
        equipamentos
            .Setup(e => e.GetByFabricanteEIdentificadorAsync(FabricanteEquipamento.Jfl, "SN-500", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha simulada de banco"));

        var excecao = await Record.ExceptionAsync(() => observer.ProcessarAsync(NovaSessao("SN-500")));

        Assert.Null(excecao);
    }

    [Fact]
    public async Task ProcessarAsync_SemNumeroSerie_NaoConsultaRepositorio()
    {
        var (equipamentos, _, observer) = NovoObservador();

        await observer.ProcessarAsync(NovaSessao(numeroSerie: null!));

        equipamentos.Verify(
            e => e.GetByFabricanteEIdentificadorAsync(It.IsAny<FabricanteEquipamento>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
