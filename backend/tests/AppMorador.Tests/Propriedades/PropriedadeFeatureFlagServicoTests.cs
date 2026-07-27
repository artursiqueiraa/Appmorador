using AppMorador.Application.Propriedades;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Propriedades;

public class PropriedadeFeatureFlagServicoTests
{
    private static (Mock<IPropriedadeRepositorio> Propriedades, Mock<IPropriedadeFeatureFlagRepositorio> Features, PropriedadeFeatureFlagServico Servico) NovoServico()
    {
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var features = new Mock<IPropriedadeFeatureFlagRepositorio>();
        var servico = new PropriedadeFeatureFlagServico(propriedades.Object, features.Object);
        return (propriedades, features, servico);
    }

    [Fact]
    public async Task ListarAtivasAsync_PropriedadeInexistente_Falha()
    {
        var (propriedades, _, servico) = NovoServico();
        propriedades.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Propriedade?)null);

        var resultado = await servico.ListarAtivasAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task DefinirAsync_PropriedadeInexistente_Falha()
    {
        var (propriedades, _, servico) = NovoServico();
        propriedades.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Propriedade?)null);

        var resultado = await servico.DefinirAsync(Guid.NewGuid(), FeatureFlag.Cameras, true, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task DefinirAsync_PropriedadeExistente_AtivaFeatureEDevolveListaAtualizada()
    {
        var (propriedades, features, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Propriedade { Id = propriedadeId, Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = Guid.NewGuid() });
        features.Setup(f => f.ListAtivasAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync([FeatureFlag.Cameras]);

        var resultado = await servico.DefinirAsync(propriedadeId, FeatureFlag.Cameras, true, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Contains(FeatureFlag.Cameras, resultado.Data!);
        features.Verify(f => f.DefinirAsync(propriedadeId, FeatureFlag.Cameras, true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
