using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Rbac;

public class UsuarioPropriedadePermissaoServicoTests
{
    private static (Mock<IUsuarioPropriedadeRepositorio> Vinculos, Mock<IUsuarioPropriedadePermissaoRepositorio> Permissoes, UsuarioPropriedadePermissaoServico Servico) NovoServico()
    {
        var vinculos = new Mock<IUsuarioPropriedadeRepositorio>();
        var permissoes = new Mock<IUsuarioPropriedadePermissaoRepositorio>();
        var servico = new UsuarioPropriedadePermissaoServico(vinculos.Object, permissoes.Object);
        return (vinculos, permissoes, servico);
    }

    [Fact]
    public async Task ListarAsync_VinculoInexistente_Falha()
    {
        var (vinculos, _, servico) = NovoServico();
        vinculos.Setup(v => v.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioPropriedade?)null);

        var resultado = await servico.ListarAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task DefinirAsync_VinculoInexistente_Falha()
    {
        var (vinculos, _, servico) = NovoServico();
        vinculos.Setup(v => v.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioPropriedade?)null);

        var resultado = await servico.DefinirAsync(Guid.NewGuid(), Guid.NewGuid(), [PermissaoFuncionalidade.CadastrarCamera], CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task DefinirAsync_VinculoExistente_SubstituiESalvaERetornaListaAtualizada()
    {
        var (vinculos, permissoes, servico) = NovoServico();
        var propriedadeId = Guid.NewGuid();
        var usuarioAlvoId = Guid.NewGuid();
        var vinculo = new UsuarioPropriedade { Id = Guid.NewGuid(), UsuarioId = usuarioAlvoId, PropriedadeId = propriedadeId, Perfil = PerfilPropriedade.Administrador, CreatedAtUtc = DateTime.UtcNow };
        vinculos.Setup(v => v.GetAsync(usuarioAlvoId, propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(vinculo);
        permissoes.Setup(p => p.ListAsync(vinculo.Id, It.IsAny<CancellationToken>())).ReturnsAsync([PermissaoFuncionalidade.CadastrarCamera]);

        var resultado = await servico.DefinirAsync(propriedadeId, usuarioAlvoId, [PermissaoFuncionalidade.CadastrarCamera], CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Contains(PermissaoFuncionalidade.CadastrarCamera, resultado.Data!);
        permissoes.Verify(p => p.SubstituirAsync(vinculo.Id, It.IsAny<IReadOnlyCollection<PermissaoFuncionalidade>>(), It.IsAny<CancellationToken>()), Times.Once);
        permissoes.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
