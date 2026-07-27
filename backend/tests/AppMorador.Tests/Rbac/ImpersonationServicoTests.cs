using AppMorador.Application.Auditoria;
using AppMorador.Application.Autenticacao;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Rbac;

public class ImpersonationServicoTests
{
    private static Usuario NovoUsuario(Guid id, string nome, RoleSistema? role = null) => new()
    {
        Id = id,
        Nome = nome,
        Email = $"{id}@teste.local",
        SenhaHash = "hash",
        RoleGlobal = role,
        CreatedAtUtc = DateTime.UtcNow,
    };

    private static (Mock<IPropriedadeRepositorio> Propriedades, Mock<IUsuarioRepositorio> Usuarios, Mock<ITokenService> Tokens,
        Mock<IAuditoriaService> Auditoria, ImpersonationServico Servico) NovoServico()
    {
        var propriedades = new Mock<IPropriedadeRepositorio>();
        var usuarios = new Mock<IUsuarioRepositorio>();
        var tokens = new Mock<ITokenService>();
        var auditoria = new Mock<IAuditoriaService>();
        tokens.Setup(t => t.ImpersonationTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        var servico = new ImpersonationServico(propriedades.Object, usuarios.Object, tokens.Object, auditoria.Object);
        return (propriedades, usuarios, tokens, auditoria, servico);
    }

    [Fact]
    public async Task IniciarAsync_PropriedadeInexistente_Falha()
    {
        var (propriedades, usuarios, _, _, servico) = NovoServico();
        var masterId = Guid.NewGuid();
        usuarios.Setup(u => u.GetByIdAsync(masterId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoUsuario(masterId, "Master", RoleSistema.Master));
        propriedades.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Propriedade?)null);

        var resultado = await servico.IniciarAsync(masterId, Guid.NewGuid(), "127.0.0.1", CancellationToken.None);

        Assert.False(resultado.Success);
    }

    [Fact]
    public async Task IniciarAsync_Sucesso_GeraTokenTemporarioEAudita()
    {
        var (propriedades, usuarios, tokens, auditoria, servico) = NovoServico();
        var masterId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var master = NovoUsuario(masterId, "Master AppMorador", RoleSistema.Master);
        var cliente = NovoUsuario(clienteId, "Carlos Henrique");
        var propriedade = new Propriedade { Id = propriedadeId, Nome = "Casa Serra", Tipo = TipoPropriedade.Residencial, ProprietarioId = clienteId };

        usuarios.Setup(u => u.GetByIdAsync(masterId, It.IsAny<CancellationToken>())).ReturnsAsync(master);
        usuarios.Setup(u => u.GetByIdAsync(clienteId, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(propriedade);
        tokens.Setup(t => t.GenerateImpersonationToken(cliente, masterId, master.Nome)).Returns("token-temporario");

        var resultado = await servico.IniciarAsync(masterId, propriedadeId, "127.0.0.1", CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal("token-temporario", resultado.Data!.AccessToken);
        Assert.Equal(900, resultado.Data.ExpiresInSeconds);
        auditoria.Verify(a => a.RegistrarAsync(
            masterId, master.Nome, TipoAcaoAuditoria.ImpersonationInicio, "Propriedade", propriedadeId.ToString(),
            It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_Sucesso_NuncaGeraRefreshToken()
    {
        var (propriedades, usuarios, tokens, _, servico) = NovoServico();
        var masterId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        usuarios.Setup(u => u.GetByIdAsync(masterId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoUsuario(masterId, "Master", RoleSistema.Master));
        usuarios.Setup(u => u.GetByIdAsync(clienteId, It.IsAny<CancellationToken>())).ReturnsAsync(NovoUsuario(clienteId, "Cliente"));
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Propriedade { Id = propriedadeId, Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = clienteId });

        await servico.IniciarAsync(masterId, propriedadeId, null, CancellationToken.None);

        tokens.Verify(t => t.GenerateRefreshToken(), Times.Never);
    }

    [Fact]
    public async Task EncerrarAsync_RegistraFimDeImpersonation()
    {
        var (_, usuarios, _, auditoria, servico) = NovoServico();
        var masterId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var master = NovoUsuario(masterId, "Master AppMorador", RoleSistema.Master);
        usuarios.Setup(u => u.GetByIdAsync(masterId, It.IsAny<CancellationToken>())).ReturnsAsync(master);

        await servico.EncerrarAsync(masterId, propriedadeId, "127.0.0.1", CancellationToken.None);

        auditoria.Verify(a => a.RegistrarAsync(
            masterId, master.Nome, TipoAcaoAuditoria.ImpersonationFim, "Propriedade", propriedadeId.ToString(),
            null, "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
