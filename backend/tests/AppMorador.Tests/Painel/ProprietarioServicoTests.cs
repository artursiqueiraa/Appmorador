using AppMorador.Application.Painel;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Painel;

file static class PropriedadeFactory
{
    public static Propriedade Nova(Guid proprietarioId) => new()
    {
        Id = Guid.NewGuid(),
        Nome = "Casa Teste",
        Tipo = TipoPropriedade.Residencial,
        ProprietarioId = proprietarioId,
    };
}

public class ProprietarioServicoTests
{
    private static Usuario NovoCliente(string nome) => new()
    {
        Id = Guid.NewGuid(),
        Nome = nome,
        Email = $"{nome.ToLowerInvariant()}@teste.local",
        SenhaHash = "hash",
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static (Mock<IUsuarioRepositorio> Usuarios, Mock<IPropriedadeRepositorio> Propriedades, ProprietarioServico Servico) NovoServico()
    {
        var usuarios = new Mock<IUsuarioRepositorio>();
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var servico = new ProprietarioServico(usuarios.Object, propriedades.Object);
        return (usuarios, propriedades, servico);
    }

    [Fact]
    public async Task ListarAsync_MapeiaContagemDePropriedadesPorCliente()
    {
        var (usuarios, propriedades, servico) = NovoServico();
        var carlos = NovoCliente("Carlos");
        var juliana = NovoCliente("Juliana");
        usuarios.Setup(u => u.ListProprietariosAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Usuario> { carlos, juliana }, 2));
        propriedades.Setup(p => p.ContarPorProprietariosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { [carlos.Id] = 3 });

        var resultado = await servico.ListarAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(2, resultado.TotalItens);
        Assert.Equal(3, resultado.Itens.Single(i => i.Id == carlos.Id).QuantidadePropriedades);
        Assert.Equal(0, resultado.Itens.Single(i => i.Id == juliana.Id).QuantidadePropriedades);
    }

    [Fact]
    public async Task ListarAsync_CalculaTotalDePaginasCorretamente()
    {
        var (usuarios, propriedades, servico) = NovoServico();
        usuarios.Setup(u => u.ListProprietariosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Usuario>(), 45));
        propriedades.Setup(p => p.ContarPorProprietariosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var resultado = await servico.ListarAsync(1, 20, null, CancellationToken.None);

        Assert.Equal(3, resultado.TotalPaginas);
    }

    [Fact]
    public async Task ListarAsync_PaginaOuTamanhoInvalidos_UsaDefaults()
    {
        var (usuarios, propriedades, servico) = NovoServico();
        usuarios.Setup(u => u.ListProprietariosAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Usuario>(), 0));
        propriedades.Setup(p => p.ContarPorProprietariosAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var resultado = await servico.ListarAsync(0, -5, null, CancellationToken.None);

        Assert.Equal(1, resultado.PaginaAtual);
        Assert.Equal(0, resultado.TotalPaginas);
        usuarios.Verify(u => u.ListProprietariosAsync(1, 20, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObterDetalheAsync_ClienteInexistente_Falha()
    {
        var (usuarios, _, servico) = NovoServico();
        usuarios.Setup(u => u.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);

        var resultado = await servico.ObterDetalheAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task ObterDetalheAsync_UsuarioInterno_Falha()
    {
        var (usuarios, _, servico) = NovoServico();
        var master = NovoCliente("Master");
        master.RoleGlobal = RoleSistema.Master;
        usuarios.Setup(u => u.GetByIdAsync(master.Id, It.IsAny<CancellationToken>())).ReturnsAsync(master);

        var resultado = await servico.ObterDetalheAsync(master.Id, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task ObterDetalheAsync_ClienteValido_RetornaPropriedades()
    {
        var (usuarios, propriedades, servico) = NovoServico();
        var carlos = NovoCliente("Carlos");
        usuarios.Setup(u => u.GetByIdAsync(carlos.Id, It.IsAny<CancellationToken>())).ReturnsAsync(carlos);
        propriedades.Setup(p => p.ListByOwnerAsync(carlos.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([PropriedadeFactory.Nova(carlos.Id), PropriedadeFactory.Nova(carlos.Id)]);

        var resultado = await servico.ObterDetalheAsync(carlos.Id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(2, resultado.Data!.Propriedades.Count);
    }
}
