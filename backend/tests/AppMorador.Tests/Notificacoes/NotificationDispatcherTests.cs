using AppMorador.Application.Notificacoes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Notificacoes;

public class NotificationDispatcherTests
{
    private static ContextoNotificacao ContextoPropriedade(Guid propriedadeId, string nomeContextual = "") => new()
    {
        PropriedadeId = propriedadeId,
        NomePropriedade = "Casa Serra",
        NomeContextual = string.IsNullOrEmpty(nomeContextual) ? null : nomeContextual,
    };

    private static ContextoNotificacao ContextoEquipamento(Guid propriedadeId, Guid equipamentoId) => new()
    {
        PropriedadeId = propriedadeId,
        NomePropriedade = "Casa Serra",
        EquipamentoId = equipamentoId,
        NomeEquipamento = "Central de alarme",
    };

    public static IEnumerable<object[]> MensagensEsperadas()
    {
        yield return new object[]
        {
            EventoNotificacaoTipo.AlarmeDisparado, "⚠️ Alarme disparado",
            "Uma área protegida foi acionada em Casa Serra", PrioridadeNotificacao.Alta, "ABRIR_APP_HISTORICO",
        };
        yield return new object[]
        {
            EventoNotificacaoTipo.SistemaArmado, "🔒 Sistema armado",
            "Sua casa está protegida", PrioridadeNotificacao.Baixa, "ABRIR_APP_INICIO",
        };
        yield return new object[]
        {
            EventoNotificacaoTipo.SistemaDesarmado, "🔓 Sistema desarmado",
            "Sua casa foi desarmada", PrioridadeNotificacao.Baixa, "ABRIR_APP_INICIO",
        };
        yield return new object[]
        {
            EventoNotificacaoTipo.ComandoAcionado, "🔓 Comando acionado",
            "Um comando foi executado em Casa Serra", PrioridadeNotificacao.Normal, "ABRIR_APP_ACESSOS",
        };
        yield return new object[]
        {
            EventoNotificacaoTipo.EntregaRecebida, "📦 Entrega recebida",
            "Uma entrega foi registrada", PrioridadeNotificacao.Normal, "ABRIR_APP_ACESSOS",
        };
    }

    [Theory]
    [MemberData(nameof(MensagensEsperadas))]
    public async Task NotificarAsync_MontaMensagemCorretaPorTipoDeEvento(
        EventoNotificacaoTipo tipo, string tituloEsperado, string corpoEsperado, PrioridadeNotificacao prioridadeEsperada, string acaoEsperada)
    {
        var propriedadeId = Guid.NewGuid();
        var notificationService = new Mock<INotificationService>();
        var debounce = new Mock<IDebounceNotificacao>();
        debounce.Setup(d => d.PodeNotificar(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<Guid>())).Returns(true);

        var dispatcher = new NotificationDispatcher(notificationService.Object, debounce.Object, NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.NotificarAsync(tipo, ContextoPropriedade(propriedadeId), CancellationToken.None);

        notificationService.Verify(s => s.EnviarParaPropriedadeAsync(
            It.Is<NotificacaoPayload>(p =>
                p.Titulo == tituloEsperado &&
                p.Corpo == corpoEsperado &&
                p.Prioridade == prioridadeEsperada &&
                p.Acao == acaoEsperada &&
                p.EventoTipo == tipo &&
                p.PropriedadeId == propriedadeId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarAsync_VisitanteAutorizado_UsaNomeContextualQuandoPresente()
    {
        var propriedadeId = Guid.NewGuid();
        var notificationService = new Mock<INotificationService>();
        var debounce = new Mock<IDebounceNotificacao>();
        debounce.Setup(d => d.PodeNotificar(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<Guid>())).Returns(true);
        var dispatcher = new NotificationDispatcher(notificationService.Object, debounce.Object, NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.NotificarAsync(EventoNotificacaoTipo.VisitanteAutorizado, ContextoPropriedade(propriedadeId, "João"), CancellationToken.None);

        notificationService.Verify(s => s.EnviarParaPropriedadeAsync(
            It.Is<NotificacaoPayload>(p => p.Corpo == "João foi autorizado a entrar"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarAsync_VisitanteAutorizado_UsaFallbackQuandoSemNomeContextual()
    {
        var propriedadeId = Guid.NewGuid();
        var notificationService = new Mock<INotificationService>();
        var debounce = new Mock<IDebounceNotificacao>();
        debounce.Setup(d => d.PodeNotificar(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<Guid>())).Returns(true);
        var dispatcher = new NotificationDispatcher(notificationService.Object, debounce.Object, NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.NotificarAsync(EventoNotificacaoTipo.VisitanteAutorizado, ContextoPropriedade(propriedadeId), CancellationToken.None);

        notificationService.Verify(s => s.EnviarParaPropriedadeAsync(
            It.Is<NotificacaoPayload>(p => p.Corpo == "Um visitante foi autorizado a entrar"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarAsync_EquipamentoOffline_UsaNomeDoEquipamentoComoContexto()
    {
        var propriedadeId = Guid.NewGuid();
        var equipamentoId = Guid.NewGuid();
        var notificationService = new Mock<INotificationService>();
        var debounce = new Mock<IDebounceNotificacao>();
        debounce.Setup(d => d.PodeNotificar(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<Guid>())).Returns(true);
        var dispatcher = new NotificationDispatcher(notificationService.Object, debounce.Object, NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.NotificarAsync(EventoNotificacaoTipo.EquipamentoOffline, ContextoEquipamento(propriedadeId, equipamentoId), CancellationToken.None);

        notificationService.Verify(s => s.EnviarParaPropriedadeAsync(
            It.Is<NotificacaoPayload>(p =>
                p.Titulo == "⚠️ Dispositivo offline" &&
                p.Corpo == "Central de alarme não está respondendo" &&
                p.Prioridade == PrioridadeNotificacao.Alta &&
                p.EquipamentoId == equipamentoId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarAsync_DebounceSuprimindo_NuncaChamaNotificationServiceNemRegistraEnvio()
    {
        var notificationService = new Mock<INotificationService>();
        var debounce = new Mock<IDebounceNotificacao>();
        debounce.Setup(d => d.PodeNotificar(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<Guid>())).Returns(false);
        var dispatcher = new NotificationDispatcher(notificationService.Object, debounce.Object, NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.NotificarAsync(EventoNotificacaoTipo.AlarmeDisparado, ContextoPropriedade(Guid.NewGuid()), CancellationToken.None);

        notificationService.Verify(s => s.EnviarParaPropriedadeAsync(It.IsAny<NotificacaoPayload>(), It.IsAny<CancellationToken>()), Times.Never);
        debounce.Verify(d => d.RegistrarEnvio(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task NotificarAsync_ChaveDeDebounce_UsaEquipamentoIdQuandoPresenteSenaoPropriedadeId()
    {
        var propriedadeId = Guid.NewGuid();
        var equipamentoId = Guid.NewGuid();
        var notificationService = new Mock<INotificationService>();
        var debounce = new Mock<IDebounceNotificacao>();
        debounce.Setup(d => d.PodeNotificar(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<Guid>())).Returns(true);
        var dispatcher = new NotificationDispatcher(notificationService.Object, debounce.Object, NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.NotificarAsync(EventoNotificacaoTipo.EquipamentoOffline, ContextoEquipamento(propriedadeId, equipamentoId), CancellationToken.None);
        await dispatcher.NotificarAsync(EventoNotificacaoTipo.AlarmeDisparado, ContextoPropriedade(propriedadeId), CancellationToken.None);

        debounce.Verify(d => d.PodeNotificar(EventoNotificacaoTipo.EquipamentoOffline, equipamentoId), Times.Once);
        debounce.Verify(d => d.RegistrarEnvio(EventoNotificacaoTipo.EquipamentoOffline, equipamentoId), Times.Once);
        debounce.Verify(d => d.PodeNotificar(EventoNotificacaoTipo.AlarmeDisparado, propriedadeId), Times.Once);
        debounce.Verify(d => d.RegistrarEnvio(EventoNotificacaoTipo.AlarmeDisparado, propriedadeId), Times.Once);
    }
}
