using AppMorador.Application.Autenticacao;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Rbac;

public class UsuarioInternoServicoTests
{
    private static (Mock<IUsuarioRepositorio> Usuarios, Mock<IPasswordHasher> Hasher, UsuarioInternoServico Servico) NovoServico()
    {
        var usuarios = new Mock<IUsuarioRepositorio>();
        var hasher = new Mock<IPasswordHasher>();
        var servico = new UsuarioInternoServico(usuarios.Object, hasher.Object);
        return (usuarios, hasher, servico);
    }

    [Fact]
    public async Task CriarAsync_EmailJaExistente_Falha()
    {
        var (usuarios, _, servico) = NovoServico();
        usuarios.Setup(u => u.GetByEmailAsync("tecnico@appmorador.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = Guid.NewGuid(), Nome = "X", Email = "tecnico@appmorador.local", SenhaHash = "h", CreatedAtUtc = DateTime.UtcNow });

        var resultado = await servico.CriarAsync(
            new CriarUsuarioInternoRequest { Nome = "Tecnico", Email = "tecnico@appmorador.local", Senha = "Senha@123", RoleGlobal = RoleSistema.Tecnico },
            CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task CriarAsync_Sucesso_HasheiaSenhaEDefineRoleGlobal()
    {
        var (usuarios, hasher, servico) = NovoServico();
        usuarios.Setup(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Usuario?)null);
        hasher.Setup(h => h.Hash("Senha@123")).Returns("hash-seguro");

        var resultado = await servico.CriarAsync(
            new CriarUsuarioInternoRequest { Nome = "Juliana Suporte", Email = "Juliana@AppMorador.local", Senha = "Senha@123", RoleGlobal = RoleSistema.Suporte },
            CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal(RoleSistema.Suporte, resultado.Data!.RoleGlobal);
        Assert.Equal("juliana@appmorador.local", resultado.Data.Email);
        Assert.True(resultado.Data.Ativo);
        usuarios.Verify(u => u.AddAsync(It.Is<Usuario>(x => x.SenhaHash == "hash-seguro"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DesativarAsync_ContaNaoInterna_Falha()
    {
        var (usuarios, _, servico) = NovoServico();
        var id = Guid.NewGuid();
        usuarios.Setup(u => u.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = id, Nome = "Cliente", Email = "cliente@x.com", SenhaHash = "h", CreatedAtUtc = DateTime.UtcNow, RoleGlobal = null });

        var resultado = await servico.DesativarAsync(id, CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task DesativarAsync_ContaInterna_DefineAtivoFalse()
    {
        var (usuarios, _, servico) = NovoServico();
        var id = Guid.NewGuid();
        var usuario = new Usuario { Id = id, Nome = "Tecnico", Email = "t@x.com", SenhaHash = "h", CreatedAtUtc = DateTime.UtcNow, RoleGlobal = RoleSistema.Tecnico };
        usuarios.Setup(u => u.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);

        var resultado = await servico.DesativarAsync(id, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.False(usuario.Ativo);
        usuarios.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
