using AppMorador.Application.Painel;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Painel;

public class DashboardOperacionalServicoTests
{
    private static (Mock<IUsuarioRepositorio> Usuarios, Mock<IPropriedadeRepositorio> Propriedades, Mock<IEquipamentoRepositorio> Equipamentos, DashboardOperacionalServico Servico)
        NovoServico()
    {
        var usuarios = new Mock<IUsuarioRepositorio>();
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var equipamentos = new Mock<IEquipamentoRepositorio>();
        var servico = new DashboardOperacionalServico(usuarios.Object, propriedades.Object, equipamentos.Object);
        return (usuarios, propriedades, equipamentos, servico);
    }

    [Fact]
    public async Task ObterAsync_SomaTotaisAPartirDosAgregados()
    {
        var (usuarios, propriedades, equipamentos, servico) = NovoServico();
        usuarios.Setup(u => u.ContarClientesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10);
        usuarios.Setup(u => u.ContarClientesPorMesAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["2026-06"] = 2, ["2026-07"] = 3 });
        propriedades.Setup(p => p.ContarPorTipoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<TipoPropriedade, int> { [TipoPropriedade.Residencial] = 7, [TipoPropriedade.Comercial] = 3 });
        equipamentos.Setup(e => e.ContarPorStatusGlobalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StatusEquipamento, int> { [StatusEquipamento.Online] = 5, [StatusEquipamento.Offline] = 2 });

        var resultado = await servico.ObterAsync(CancellationToken.None);

        Assert.Equal(10, resultado.TotalClientes);
        Assert.Equal(10, resultado.TotalPropriedades);
        Assert.Equal(7, resultado.TotalEquipamentos);
        Assert.Equal(2, resultado.TotalEquipamentosOffline);
    }

    [Fact]
    public async Task ObterAsync_SemEquipamentosOffline_TotalZero()
    {
        var (usuarios, propriedades, equipamentos, servico) = NovoServico();
        usuarios.Setup(u => u.ContarClientesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        usuarios.Setup(u => u.ContarClientesPorMesAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, int>());
        propriedades.Setup(p => p.ContarPorTipoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<TipoPropriedade, int>());
        equipamentos.Setup(e => e.ContarPorStatusGlobalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<StatusEquipamento, int> { [StatusEquipamento.Online] = 5 });

        var resultado = await servico.ObterAsync(CancellationToken.None);

        Assert.Equal(0, resultado.TotalEquipamentosOffline);
    }

    [Fact]
    public async Task ObterAsync_NovosClientesPorMesOrdenadoCrescente()
    {
        var (usuarios, propriedades, equipamentos, servico) = NovoServico();
        usuarios.Setup(u => u.ContarClientesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        usuarios.Setup(u => u.ContarClientesPorMesAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["2026-07"] = 3, ["2026-05"] = 1, ["2026-06"] = 2 });
        propriedades.Setup(p => p.ContarPorTipoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<TipoPropriedade, int>());
        equipamentos.Setup(e => e.ContarPorStatusGlobalAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<StatusEquipamento, int>());

        var resultado = await servico.ObterAsync(CancellationToken.None);

        Assert.Equal(["2026-05", "2026-06", "2026-07"], resultado.NovosClientesPorMes.Select(i => i.Mes));
    }
}
