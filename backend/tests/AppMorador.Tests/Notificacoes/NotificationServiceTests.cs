using AppMorador.Application.Notificacoes;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Notificacoes;

public class NotificationServiceTests
{
    private static Propriedade NovaPropriedade(Guid id, Guid proprietarioId) => new()
    {
        Id = id,
        Nome = "Casa Serra",
        Tipo = TipoPropriedade.Residencial,
        ProprietarioId = proprietarioId,
    };

    private static DispositivoPush NovoDispositivo(
        Guid usuarioId, string token, bool ativo = true, bool notificarAlertas = true, bool notificarAtividades = true, bool notificarGeral = true) => new()
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Plataforma = PlataformaDispositivo.Android,
            Token = token,
            Ativo = ativo,
            NotificarAlertas = notificarAlertas,
            NotificarAtividades = notificarAtividades,
            NotificarGeral = notificarGeral,
            UltimoUsoUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private static NotificacaoPayload NovoPayload(Guid propriedadeId, EventoNotificacaoTipo tipo = EventoNotificacaoTipo.AlarmeDisparado) => new()
    {
        Titulo = "⚠️ Alarme disparado",
        Corpo = "Uma área protegida foi acionada em Casa Serra",
        EventoTipo = tipo,
        PropriedadeId = propriedadeId,
        Acao = "ABRIR_APP_HISTORICO",
        Prioridade = PrioridadeNotificacao.Alta,
    };

    [Fact]
    public async Task EnviarParaPropriedadeAsync_PropriedadeInexistente_NaoChamaProvider()
    {
        var propriedades = new Mock<IPropriedadeRepositorio>();
        propriedades.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Propriedade?)null);
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        var provider = new Mock<INotificationProvider>();
        var service = new NotificationService(propriedades.Object, dispositivos.Object, provider.Object, NullLogger<NotificationService>.Instance);

        await service.EnviarParaPropriedadeAsync(NovoPayload(Guid.NewGuid()), CancellationToken.None);

        provider.Verify(p => p.EnviarAsync(It.IsAny<NotificacaoPayload>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnviarParaPropriedadeAsync_SemDispositivosAtivos_NaoChamaProviderNemLancaExcecao()
    {
        var proprietarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var propriedades = new Mock<IPropriedadeRepositorio>();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId, proprietarioId));
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.ListAtivosByUsuarioAsync(proprietarioId, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<DispositivoPush>());
        var provider = new Mock<INotificationProvider>();
        var service = new NotificationService(propriedades.Object, dispositivos.Object, provider.Object, NullLogger<NotificationService>.Instance);

        await service.EnviarParaPropriedadeAsync(NovoPayload(propriedadeId), CancellationToken.None);

        provider.Verify(p => p.EnviarAsync(It.IsAny<NotificacaoPayload>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnviarParaPropriedadeAsync_DoisDispositivosAtivos_AmbosRecebemToken()
    {
        var proprietarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var dispositivo1 = NovoDispositivo(proprietarioId, "token-1");
        var dispositivo2 = NovoDispositivo(proprietarioId, "token-2");

        var propriedades = new Mock<IPropriedadeRepositorio>();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId, proprietarioId));
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.ListAtivosByUsuarioAsync(proprietarioId, It.IsAny<CancellationToken>())).ReturnsAsync([dispositivo1, dispositivo2]);
        var provider = new Mock<INotificationProvider>();
        provider.Setup(p => p.EnviarAsync(It.IsAny<NotificacaoPayload>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEnvioNotificacao { Sucesso = true, TokensComSucesso = ["token-1", "token-2"] });
        var service = new NotificationService(propriedades.Object, dispositivos.Object, provider.Object, NullLogger<NotificationService>.Instance);

        await service.EnviarParaPropriedadeAsync(NovoPayload(propriedadeId), CancellationToken.None);

        provider.Verify(p => p.EnviarAsync(
            It.IsAny<NotificacaoPayload>(),
            It.Is<IReadOnlyList<string>>(tokens => tokens.Count == 2 && tokens.Contains("token-1") && tokens.Contains("token-2")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarParaPropriedadeAsync_DispositivoComCanalDesabilitado_NaoRecebeToken()
    {
        var proprietarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var dispositivoComAlertas = NovoDispositivo(proprietarioId, "token-alertas", notificarAlertas: true);
        var dispositivoSemAlertas = NovoDispositivo(proprietarioId, "token-sem-alertas", notificarAlertas: false);

        var propriedades = new Mock<IPropriedadeRepositorio>();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId, proprietarioId));
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.ListAtivosByUsuarioAsync(proprietarioId, It.IsAny<CancellationToken>())).ReturnsAsync([dispositivoComAlertas, dispositivoSemAlertas]);
        var provider = new Mock<INotificationProvider>();
        provider.Setup(p => p.EnviarAsync(It.IsAny<NotificacaoPayload>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEnvioNotificacao { Sucesso = true, TokensComSucesso = ["token-alertas"] });
        var service = new NotificationService(propriedades.Object, dispositivos.Object, provider.Object, NullLogger<NotificationService>.Instance);

        await service.EnviarParaPropriedadeAsync(NovoPayload(propriedadeId, EventoNotificacaoTipo.AlarmeDisparado), CancellationToken.None);

        provider.Verify(p => p.EnviarAsync(
            It.IsAny<NotificacaoPayload>(),
            It.Is<IReadOnlyList<string>>(tokens => tokens.Count == 1 && tokens.Contains("token-alertas")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarParaPropriedadeAsync_TokenInvalidoRetornadoPeloProvider_DesativaSomenteEsseDispositivo()
    {
        var proprietarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var dispositivoValido = NovoDispositivo(proprietarioId, "token-valido");
        var dispositivoInvalido = NovoDispositivo(proprietarioId, "token-invalido");

        var propriedades = new Mock<IPropriedadeRepositorio>();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId, proprietarioId));
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.ListAtivosByUsuarioAsync(proprietarioId, It.IsAny<CancellationToken>())).ReturnsAsync([dispositivoValido, dispositivoInvalido]);
        var provider = new Mock<INotificationProvider>();
        provider.Setup(p => p.EnviarAsync(It.IsAny<NotificacaoPayload>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEnvioNotificacao
            {
                Sucesso = true,
                TokensComSucesso = ["token-valido"],
                TokensInvalidos = ["token-invalido"],
            });
        var service = new NotificationService(propriedades.Object, dispositivos.Object, provider.Object, NullLogger<NotificationService>.Instance);

        await service.EnviarParaPropriedadeAsync(NovoPayload(propriedadeId), CancellationToken.None);

        Assert.False(dispositivoInvalido.Ativo);
        Assert.True(dispositivoValido.Ativo);
        dispositivos.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarParaPropriedadeAsync_ProviderIndisponivel_NaoLancaExcecaoENaoDesativaDispositivos()
    {
        var proprietarioId = Guid.NewGuid();
        var propriedadeId = Guid.NewGuid();
        var dispositivo = NovoDispositivo(proprietarioId, "token-1");

        var propriedades = new Mock<IPropriedadeRepositorio>();
        propriedades.Setup(p => p.GetByIdAsync(propriedadeId, It.IsAny<CancellationToken>())).ReturnsAsync(NovaPropriedade(propriedadeId, proprietarioId));
        var dispositivos = new Mock<IDispositivoPushRepositorio>();
        dispositivos.Setup(d => d.ListAtivosByUsuarioAsync(proprietarioId, It.IsAny<CancellationToken>())).ReturnsAsync([dispositivo]);
        var provider = new Mock<INotificationProvider>();
        provider.Setup(p => p.EnviarAsync(It.IsAny<NotificacaoPayload>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEnvioNotificacao { Sucesso = false, Erro = "Falha ao enviar notificação." });
        var service = new NotificationService(propriedades.Object, dispositivos.Object, provider.Object, NullLogger<NotificationService>.Instance);

        await service.EnviarParaPropriedadeAsync(NovoPayload(propriedadeId), CancellationToken.None);

        Assert.True(dispositivo.Ativo);
        dispositivos.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
