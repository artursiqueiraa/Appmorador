using AppMorador.Application.Equipamentos;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Equipamentos;

public class ModeloEquipamentoServicoTests
{
    private static (Mock<IModeloEquipamentoRepositorio> Modelos, ModeloEquipamentoServico Servico) NovoServico()
    {
        var modelos = new Mock<IModeloEquipamentoRepositorio>();
        modelos.Setup(m => m.ListCapacidadesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var servico = new ModeloEquipamentoServico(modelos.Object);
        return (modelos, servico);
    }

    [Fact]
    public async Task CriarAsync_ModeloJaExistente_ReaproveitaSemCriarNovo()
    {
        var (modelos, servico) = NovoServico();
        var existente = new ModeloEquipamento { Id = Guid.NewGuid(), Fabricante = FabricanteEquipamento.Intelbras, Nome = "AMT 8000", CreatedAtUtc = DateTime.UtcNow };
        modelos.Setup(m => m.GetByFabricanteENomeAsync(FabricanteEquipamento.Intelbras, "AMT 8000", It.IsAny<CancellationToken>())).ReturnsAsync(existente);

        var resultado = await servico.CriarAsync(new CriarModeloEquipamentoRequest { Fabricante = FabricanteEquipamento.Intelbras, Nome = "AMT 8000" }, CancellationToken.None);

        Assert.Equal(existente.Id, resultado.Id);
        modelos.Verify(m => m.AddAsync(It.IsAny<ModeloEquipamento>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_ModeloNovo_Cria()
    {
        var (modelos, servico) = NovoServico();
        modelos.Setup(m => m.GetByFabricanteENomeAsync(FabricanteEquipamento.Jfl, "Active 100 Bus", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ModeloEquipamento?)null);

        var resultado = await servico.CriarAsync(new CriarModeloEquipamentoRequest { Fabricante = FabricanteEquipamento.Jfl, Nome = "Active 100 Bus" }, CancellationToken.None);

        Assert.Equal("Active 100 Bus", resultado.Nome);
        modelos.Verify(m => m.AddAsync(It.IsAny<ModeloEquipamento>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DefinirCapacidadesAsync_ModeloInexistente_Falha()
    {
        var (modelos, servico) = NovoServico();
        modelos.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ModeloEquipamento?)null);

        var resultado = await servico.DefinirCapacidadesAsync(Guid.NewGuid(), [EquipamentoCapacidade.Armar], CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task DefinirCapacidadesAsync_ModeloExistente_SubstituiCapacidades()
    {
        var (modelos, servico) = NovoServico();
        var modeloId = Guid.NewGuid();
        var modelo = new ModeloEquipamento { Id = modeloId, Fabricante = FabricanteEquipamento.ControlId, Nome = "iDAccess Nano", CreatedAtUtc = DateTime.UtcNow };
        modelos.Setup(m => m.GetByIdAsync(modeloId, It.IsAny<CancellationToken>())).ReturnsAsync(modelo);

        var resultado = await servico.DefinirCapacidadesAsync(modeloId, [EquipamentoCapacidade.Face, EquipamentoCapacidade.Tag], CancellationToken.None);

        Assert.True(resultado.Success);
        modelos.Verify(m => m.SubstituirCapacidadesAsync(modeloId, It.Is<IReadOnlyCollection<EquipamentoCapacidade>>(c => c.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
    }
}
