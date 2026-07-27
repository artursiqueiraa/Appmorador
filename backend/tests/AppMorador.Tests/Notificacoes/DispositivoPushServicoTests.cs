using AppMorador.Application.Notificacoes;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Notificacoes;

public class DispositivoPushServicoTests
{
    private static RegistrarDispositivoPushRequest NovoRequest(string token) => new()
    {
        Plataforma = PlataformaDispositivo.Android,
        Token = token,
        Modelo = "Pixel 8",
        VersaoApp = "1.0.0",
    };

    [Fact]
    public async Task RegistrarAsync_TokenNovo_CriaDispositivo()
    {
        var usuarioId = Guid.NewGuid();
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.GetByTokenAsync("token-novo", It.IsAny<CancellationToken>())).ReturnsAsync((DispositivoPush?)null);
        var servico = new DispositivoPushServico(dispositivos.Object);

        var response = await servico.RegistrarAsync(usuarioId, NovoRequest("token-novo"), CancellationToken.None);

        dispositivos.Verify(d => d.AddAsync(It.Is<DispositivoPush>(dp => dp.Token == "token-novo" && dp.UsuarioId == usuarioId && dp.Ativo), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(response.Ativo);
    }

    [Fact]
    public async Task RegistrarAsync_TokenJaExistenteDeOutroUsuario_ReassumeParaUsuarioAtual()
    {
        // Sprint 19 — cenario de reinstalacao/login com outra conta no mesmo aparelho:
        // o mesmo token fisico nao pode ficar "fantasma" registrado para a conta antiga.
        var usuarioAntigoId = Guid.NewGuid();
        var usuarioNovoId = Guid.NewGuid();
        var existente = new DispositivoPush
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioAntigoId,
            Plataforma = PlataformaDispositivo.Android,
            Token = "token-compartilhado",
            Ativo = false,
            UltimoUsoUtc = DateTime.UtcNow.AddDays(-10),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
        };
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.GetByTokenAsync("token-compartilhado", It.IsAny<CancellationToken>())).ReturnsAsync(existente);
        var servico = new DispositivoPushServico(dispositivos.Object);

        await servico.RegistrarAsync(usuarioNovoId, NovoRequest("token-compartilhado"), CancellationToken.None);

        Assert.Equal(usuarioNovoId, existente.UsuarioId);
        Assert.True(existente.Ativo);
        dispositivos.Verify(d => d.AddAsync(It.IsAny<DispositivoPush>(), It.IsAny<CancellationToken>()), Times.Never);
        dispositivos.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarTokenAsync_DispositivoDeOutroUsuario_FalhaSemAlterarNada()
    {
        var donoId = Guid.NewGuid();
        var outroUsuarioId = Guid.NewGuid();
        var dispositivoId = Guid.NewGuid();
        var dispositivo = new DispositivoPush
        {
            Id = dispositivoId,
            UsuarioId = donoId,
            Plataforma = PlataformaDispositivo.Android,
            Token = "token-original",
            Ativo = true,
            UltimoUsoUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.GetByIdAsync(dispositivoId, It.IsAny<CancellationToken>())).ReturnsAsync(dispositivo);
        var servico = new DispositivoPushServico(dispositivos.Object);

        var resultado = await servico.AtualizarTokenAsync(outroUsuarioId, dispositivoId, new AtualizarDispositivoPushRequest { Token = "token-novo" }, CancellationToken.None);

        Assert.False(resultado.Success);
        Assert.Equal("token-original", dispositivo.Token);
        dispositivos.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarTokenAsync_DonoCorreto_AtualizaTokenEReativa()
    {
        var donoId = Guid.NewGuid();
        var dispositivoId = Guid.NewGuid();
        var dispositivo = new DispositivoPush
        {
            Id = dispositivoId,
            UsuarioId = donoId,
            Plataforma = PlataformaDispositivo.Android,
            Token = "token-antigo",
            Ativo = false,
            UltimoUsoUtc = DateTime.UtcNow.AddDays(-30),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-30),
        };
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.GetByIdAsync(dispositivoId, It.IsAny<CancellationToken>())).ReturnsAsync(dispositivo);
        var servico = new DispositivoPushServico(dispositivos.Object);

        var resultado = await servico.AtualizarTokenAsync(donoId, dispositivoId, new AtualizarDispositivoPushRequest { Token = "token-rotacionado" }, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.Equal("token-rotacionado", dispositivo.Token);
        Assert.True(dispositivo.Ativo);
    }

    [Fact]
    public async Task DesativarAsync_DispositivoDeOutroUsuario_RetornaOkENaoAlteraNada()
    {
        // Sprint 19 — DesativarAsync e chamado no logout e precisa ser idempotente,
        // mesmo quando o dispositivo pertence a outra conta ou nao existe mais.
        var dispositivoId = Guid.NewGuid();
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.GetByIdAsync(dispositivoId, It.IsAny<CancellationToken>())).ReturnsAsync((DispositivoPush?)null);
        var servico = new DispositivoPushServico(dispositivos.Object);

        var resultado = await servico.DesativarAsync(Guid.NewGuid(), dispositivoId, CancellationToken.None);

        Assert.True(resultado.Success);
        dispositivos.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DesativarAsync_DonoCorreto_MarcaAtivoFalse()
    {
        var donoId = Guid.NewGuid();
        var dispositivoId = Guid.NewGuid();
        var dispositivo = new DispositivoPush
        {
            Id = dispositivoId,
            UsuarioId = donoId,
            Plataforma = PlataformaDispositivo.Android,
            Token = "token-1",
            Ativo = true,
            UltimoUsoUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.GetByIdAsync(dispositivoId, It.IsAny<CancellationToken>())).ReturnsAsync(dispositivo);
        var servico = new DispositivoPushServico(dispositivos.Object);

        var resultado = await servico.DesativarAsync(donoId, dispositivoId, CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.False(dispositivo.Ativo);
    }

    [Fact]
    public async Task AtualizarPreferenciasAsync_DonoCorreto_AtualizaOsTresCanais()
    {
        var donoId = Guid.NewGuid();
        var dispositivoId = Guid.NewGuid();
        var dispositivo = new DispositivoPush
        {
            Id = dispositivoId,
            UsuarioId = donoId,
            Plataforma = PlataformaDispositivo.Android,
            Token = "token-1",
            Ativo = true,
            UltimoUsoUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.GetByIdAsync(dispositivoId, It.IsAny<CancellationToken>())).ReturnsAsync(dispositivo);
        var servico = new DispositivoPushServico(dispositivos.Object);

        var resultado = await servico.AtualizarPreferenciasAsync(
            donoId, dispositivoId,
            new AtualizarPreferenciasDispositivoPushRequest { NotificarAlertas = true, NotificarAtividades = false, NotificarGeral = false },
            CancellationToken.None);

        Assert.True(resultado.Success);
        Assert.True(dispositivo.NotificarAlertas);
        Assert.False(dispositivo.NotificarAtividades);
        Assert.False(dispositivo.NotificarGeral);
    }
}
