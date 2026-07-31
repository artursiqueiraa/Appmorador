using AppMorador.Application.Painel.Diagnostico;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Painel;

public class DiagnosticoServicoTests
{
    private static (Mock<IDiagnosticoEquipamentoRepositorio> Repositorio, DiagnosticoServico Servico) NovoServico()
    {
        var repositorio = new Mock<IDiagnosticoEquipamentoRepositorio>();
        var servico = new DiagnosticoServico(repositorio.Object);
        return (repositorio, servico);
    }

    private static DiagnosticoEquipamentoDados NovoDado(
        DateTime? ultimaSincronizacaoUtc, DateTime? statusCentralCapturadoEmUtc) => new()
        {
            EquipamentoId = Guid.NewGuid(),
            EquipamentoNome = "Central Teste",
            Fabricante = FabricanteEquipamento.Jfl,
            PropriedadeId = Guid.NewGuid(),
            PropriedadeNome = "Casa Teste",
            Status = StatusEquipamento.Online,
            EstadoOperacional = EstadoOperacionalEquipamento.Ativo,
            UltimaSincronizacaoUtc = ultimaSincronizacaoUtc,
            StatusCentralCapturadoEmUtc = statusCentralCapturadoEmUtc,
            StatusCentralTemProblemaAtivo = false,
            QuantidadeEventosRecentes = 0,
        };

    [Fact]
    public async Task ObterStatusEquipamentosAsync_PaginaInvalida_UsaPadrao()
    {
        var (repositorio, servico) = NovoServico();
        repositorio.Setup(r => r.ListarStatusAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DiagnosticoEquipamentoDados>(), 0));

        var resultado = await servico.ObterStatusEquipamentosAsync(0, 0, CancellationToken.None);

        Assert.Equal(1, resultado.PaginaAtual);
        Assert.Equal(0, resultado.TotalItens);
        repositorio.Verify(r => r.ListarStatusAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterStatusEquipamentosAsync_UltimoPing_EscolheOMaisRecenteEntreSincronizacaoEStatusCentral()
    {
        var (repositorio, servico) = NovoServico();
        var sincronizacao = new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);
        var statusCentral = new DateTime(2026, 7, 25, 8, 0, 0, DateTimeKind.Utc);
        repositorio.Setup(r => r.ListarStatusAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DiagnosticoEquipamentoDados> { NovoDado(sincronizacao, statusCentral) }, 1));

        var resultado = await servico.ObterStatusEquipamentosAsync(1, 20, CancellationToken.None);

        Assert.Equal(statusCentral, resultado.Itens[0].UltimoPingUtc);
    }

    [Fact]
    public async Task ObterStatusEquipamentosAsync_SemNenhumPing_RetornaNulo()
    {
        var (repositorio, servico) = NovoServico();
        repositorio.Setup(r => r.ListarStatusAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DiagnosticoEquipamentoDados> { NovoDado(null, null) }, 1));

        var resultado = await servico.ObterStatusEquipamentosAsync(1, 20, CancellationToken.None);

        Assert.Null(resultado.Itens[0].UltimoPingUtc);
    }

    [Fact]
    public async Task ObterStatusEquipamentosAsync_TamanhoPaginaAcimaDoLimite_UsaPadrao()
    {
        var (repositorio, servico) = NovoServico();
        repositorio.Setup(r => r.ListarStatusAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<DiagnosticoEquipamentoDados>(), 0));

        await servico.ObterStatusEquipamentosAsync(1, 500, CancellationToken.None);

        repositorio.Verify(r => r.ListarStatusAsync(1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}
