using AppMorador.Application.Auditoria;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Auditoria;

public class AuditoriaServiceTests
{
    private static (Mock<IAuditoriaMasterRepositorio> Repositorio, AuditoriaService Servico) NovoServico()
    {
        var repositorio = new Mock<IAuditoriaMasterRepositorio>();
        var servico = new AuditoriaService(repositorio.Object, NullLogger<AuditoriaService>.Instance);
        return (repositorio, servico);
    }

    [Fact]
    public async Task RegistrarAsync_Login_PersisteRegistroComOsCamposCorretos()
    {
        var (repositorio, servico) = NovoServico();
        var usuarioId = Guid.NewGuid();
        AuditoriaMaster? capturado = null;
        repositorio.Setup(r => r.AddAsync(It.IsAny<AuditoriaMaster>(), It.IsAny<CancellationToken>()))
            .Callback<AuditoriaMaster, CancellationToken>((a, _) => capturado = a)
            .Returns(Task.CompletedTask);

        await servico.RegistrarAsync(usuarioId, "Master AppMorador", TipoAcaoAuditoria.Login, null, null, null, "127.0.0.1", CancellationToken.None);

        Assert.NotNull(capturado);
        Assert.Equal(usuarioId, capturado!.UsuarioId);
        Assert.Equal(TipoAcaoAuditoria.Login, capturado.Acao);
        Assert.Equal("127.0.0.1", capturado.IpAddress);
        repositorio.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_RepositorioLancaExcecao_NuncaPropaga()
    {
        var (repositorio, servico) = NovoServico();
        repositorio.Setup(r => r.AddAsync(It.IsAny<AuditoriaMaster>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("falha de banco"));

        var excecao = await Record.ExceptionAsync(() =>
            servico.RegistrarAsync(Guid.NewGuid(), "Master", TipoAcaoAuditoria.Login, null, null, null, null, CancellationToken.None));

        Assert.Null(excecao);
    }

    [Fact]
    public async Task RegistrarFalhaAutorizacaoAsync_UsuarioAnonimo_RegistraSemUsuarioId()
    {
        var (repositorio, servico) = NovoServico();
        AuditoriaMaster? capturado = null;
        repositorio.Setup(r => r.AddAsync(It.IsAny<AuditoriaMaster>(), It.IsAny<CancellationToken>()))
            .Callback<AuditoriaMaster, CancellationToken>((a, _) => capturado = a)
            .Returns(Task.CompletedTask);

        await servico.RegistrarFalhaAutorizacaoAsync(null, "/api/usuarios-internos", "10.0.0.1", CancellationToken.None);

        Assert.NotNull(capturado);
        Assert.Equal(TipoAcaoAuditoria.FalhaAutorizacao, capturado!.Acao);
        Assert.Equal("Endpoint", capturado.Entidade);
        Assert.Equal("/api/usuarios-internos", capturado.EntidadeId);
    }

    [Fact]
    public async Task ListarRecentesAsync_DelegaAoRepositorio()
    {
        var (repositorio, servico) = NovoServico();
        repositorio.Setup(r => r.ListRecentesAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await servico.ListarRecentesAsync(10, CancellationToken.None);

        Assert.Empty(resultado);
        repositorio.Verify(r => r.ListRecentesAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
