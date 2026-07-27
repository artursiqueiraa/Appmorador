using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Rbac;

public class PermissaoServiceTests
{
    private static (Mock<IUsuarioPropriedadeRepositorio> Vinculos, Mock<IUsuarioPropriedadePermissaoRepositorio> Permissoes,
        Mock<IPropriedadeFeatureFlagRepositorio> Features, Mock<IEquipamentoRepositorio> Equipamentos,
        Mock<IModeloEquipamentoRepositorio> Modelos, PermissaoService Servico) NovoServico()
    {
        var vinculos = new Mock<IUsuarioPropriedadeRepositorio>();
        var permissoes = new Mock<IUsuarioPropriedadePermissaoRepositorio>();
        var features = new Mock<IPropriedadeFeatureFlagRepositorio>();
        var equipamentos = new Mock<IEquipamentoRepositorio>();
        var modelos = new Mock<IModeloEquipamentoRepositorio>();
        var servico = new PermissaoService(vinculos.Object, permissoes.Object, features.Object, equipamentos.Object, modelos.Object);
        return (vinculos, permissoes, features, equipamentos, modelos, servico);
    }

    [Fact]
    public async Task TemPermissaoAsync_SemVinculo_RetornaFalse()
    {
        var (vinculos, _, _, _, _, servico) = NovoServico();
        var usuarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        vinculos.Setup(v => v.GetAsync(usuarioId, propriedadeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioPropriedade?)null);

        var resultado = await servico.TemPermissaoAsync(usuarioId, propriedadeId, PermissaoFuncionalidade.VerCameras, CancellationToken.None);

        Assert.False(resultado);
    }

    [Fact]
    public async Task TemPermissaoAsync_VinculoInativo_RetornaFalse()
    {
        var (vinculos, permissoes, _, _, _, servico) = NovoServico();
        var usuarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var vinculo = new UsuarioPropriedade { Id = Guid.NewGuid(), UsuarioId = usuarioId, PropriedadeId = propriedadeId, Perfil = PerfilPropriedade.Administrador, CreatedAtUtc = DateTime.UtcNow, Ativo = false };
        vinculos.Setup(v => v.GetAsync(usuarioId, propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(vinculo);

        var resultado = await servico.TemPermissaoAsync(usuarioId, propriedadeId, PermissaoFuncionalidade.VerCameras, CancellationToken.None);

        Assert.False(resultado);
        permissoes.Verify(p => p.TemPermissaoAsync(It.IsAny<Guid>(), It.IsAny<PermissaoFuncionalidade>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TemPermissaoAsync_VinculoAtivoComPermissao_RetornaTrue()
    {
        var (vinculos, permissoes, _, _, _, servico) = NovoServico();
        var usuarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var vinculo = new UsuarioPropriedade { Id = Guid.NewGuid(), UsuarioId = usuarioId, PropriedadeId = propriedadeId, Perfil = PerfilPropriedade.Administrador, CreatedAtUtc = DateTime.UtcNow, Ativo = true };
        vinculos.Setup(v => v.GetAsync(usuarioId, propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(vinculo);
        permissoes.Setup(p => p.TemPermissaoAsync(vinculo.Id, PermissaoFuncionalidade.VerCameras, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resultado = await servico.TemPermissaoAsync(usuarioId, propriedadeId, PermissaoFuncionalidade.VerCameras, CancellationToken.None);

        Assert.True(resultado);
    }

    [Fact]
    public async Task ListarPermissoesAsync_SemVinculo_RetornaListaVazia()
    {
        var (vinculos, _, _, _, _, servico) = NovoServico();
        var usuarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        vinculos.Setup(v => v.GetAsync(usuarioId, propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioPropriedade?)null);

        var resultado = await servico.ListarPermissoesAsync(usuarioId, propriedadeId, CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task PropriedadeTemFeatureAsync_DelegaAoRepositorio()
    {
        var (_, _, features, _, _, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        features.Setup(f => f.TemFeatureAtivaAsync(propriedadeId, FeatureFlag.Cameras, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resultado = await servico.PropriedadeTemFeatureAsync(propriedadeId, FeatureFlag.Cameras, CancellationToken.None);

        Assert.True(resultado);
    }

    [Fact]
    public async Task ListarCapacidadesAsync_EquipamentoSemModelo_RetornaListaVazia()
    {
        var (_, _, _, equipamentos, modelos, servico) = NovoServico();
        var equipamentoId = Guid.NewGuid();
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Equipamento
            {
                Id = equipamentoId,
                PropriedadeId = Guid.NewGuid(),
                Nome = "Central",
                Fabricante = FabricanteEquipamento.Jfl,
                Status = StatusEquipamento.Desconhecido,
                CreatedAtUtc = DateTime.UtcNow,
            });

        var resultado = await servico.ListarCapacidadesAsync(equipamentoId, CancellationToken.None);

        Assert.Empty(resultado);
        modelos.Verify(m => m.ListCapacidadesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarCapacidadesAsync_EquipamentoComModelo_RetornaCapacidadesDoModelo()
    {
        var (_, _, _, equipamentos, modelos, servico) = NovoServico();
        var equipamentoId = Guid.NewGuid();
        var modeloId = Guid.NewGuid();
        equipamentos.Setup(e => e.GetByIdAsync(equipamentoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Equipamento
            {
                Id = equipamentoId,
                PropriedadeId = Guid.NewGuid(),
                Nome = "Central",
                Fabricante = FabricanteEquipamento.Jfl,
                Status = StatusEquipamento.Desconhecido,
                CreatedAtUtc = DateTime.UtcNow,
                ModeloEquipamentoId = modeloId,
            });
        modelos.Setup(m => m.ListCapacidadesAsync(modeloId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([EquipamentoCapacidade.Armar, EquipamentoCapacidade.Desarmar]);

        var resultado = await servico.ListarCapacidadesAsync(equipamentoId, CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Contains(EquipamentoCapacidade.Armar, resultado);
    }
}
