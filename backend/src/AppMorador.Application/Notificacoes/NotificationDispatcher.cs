using Microsoft.Extensions.Logging;

namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023) — implementação da decisão de negócio "notificar ou não,
/// com qual mensagem". A tabela de mensagens (Fase 4 da missão) é fixa aqui, não
/// configurável — mudar o texto de uma notificação é uma decisão de produto, não
/// uma configuração de runtime.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationService _notificationService;
    private readonly IDebounceNotificacao _debounce;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(INotificationService notificationService, IDebounceNotificacao debounce, ILogger<NotificationDispatcher> logger)
    {
        _notificationService = notificationService;
        _debounce = debounce;
        _logger = logger;
    }

    public async Task NotificarAsync(EventoNotificacaoTipo tipo, ContextoNotificacao contexto, CancellationToken cancellationToken)
    {
        // Sprint 19 (Fase 8.2) — debounce por (tipo, equipamento OU propriedade): um
        // equipamento oscilando offline/online repetidamente, ou um comando acionado
        // várias vezes seguidas, gera só a primeira notificação por minuto.
        var chaveDebounce = contexto.EquipamentoId ?? contexto.PropriedadeId;
        if (!_debounce.PodeNotificar(tipo, chaveDebounce))
        {
            _logger.LogInformation("Notificacao {Tipo} suprimida por debounce (chave {Chave})", tipo, chaveDebounce);
            return;
        }

        var mensagem = MontarMensagem(tipo, contexto);
        if (mensagem is null)
        {
            // Regra de Ouro: só existe aqui um caminho sem mensagem — EquipamentoOnline
            // nunca chega a ser chamado (nem está no enum), mas o switch precisa ser
            // exaustivo; nenhum outro tipo cai neste ramo.
            return;
        }

        var payload = new NotificacaoPayload
        {
            Titulo = mensagem.Value.Titulo,
            Corpo = mensagem.Value.Corpo,
            EventoTipo = tipo,
            PropriedadeId = contexto.PropriedadeId,
            EquipamentoId = contexto.EquipamentoId,
            Acao = mensagem.Value.Acao,
            Prioridade = mensagem.Value.Prioridade,
        };

        await _notificationService.EnviarParaPropriedadeAsync(payload, cancellationToken).ConfigureAwait(false);
        _debounce.RegistrarEnvio(tipo, chaveDebounce);
    }

    private static (string Titulo, string Corpo, PrioridadeNotificacao Prioridade, string Acao)? MontarMensagem(EventoNotificacaoTipo tipo, ContextoNotificacao contexto) =>
        tipo switch
        {
            EventoNotificacaoTipo.AlarmeDisparado => (
                "⚠️ Alarme disparado",
                $"Uma área protegida foi acionada em {contexto.NomePropriedade}",
                PrioridadeNotificacao.Alta,
                "ABRIR_APP_HISTORICO"),

            EventoNotificacaoTipo.SistemaArmado => (
                "🔒 Sistema armado",
                "Sua casa está protegida",
                PrioridadeNotificacao.Baixa,
                "ABRIR_APP_INICIO"),

            EventoNotificacaoTipo.SistemaDesarmado => (
                "🔓 Sistema desarmado",
                "Sua casa foi desarmada",
                PrioridadeNotificacao.Baixa,
                "ABRIR_APP_INICIO"),

            // Sprint 19 — o backend nunca chama isso de "portão": nomes amigáveis de
            // comando (Sprint 17, pgmLabels.ts) são uma preferência só do celular, o
            // backend só sabe que um comando genérico foi acionado com sucesso.
            EventoNotificacaoTipo.ComandoAcionado => (
                "🔓 Comando acionado",
                $"Um comando foi executado em {contexto.NomePropriedade}",
                PrioridadeNotificacao.Normal,
                "ABRIR_APP_ACESSOS"),

            EventoNotificacaoTipo.VisitanteAutorizado => (
                "🔔 Visitante autorizado",
                $"{contexto.NomeContextual ?? "Um visitante"} foi autorizado a entrar",
                PrioridadeNotificacao.Normal,
                "ABRIR_APP_ACESSOS"),

            EventoNotificacaoTipo.EntregaRecebida => (
                "📦 Entrega recebida",
                "Uma entrega foi registrada",
                PrioridadeNotificacao.Normal,
                "ABRIR_APP_ACESSOS"),

            EventoNotificacaoTipo.EquipamentoOffline => (
                "⚠️ Dispositivo offline",
                $"{contexto.NomeEquipamento ?? "Um dispositivo"} não está respondendo",
                PrioridadeNotificacao.Alta,
                "ABRIR_APP_HISTORICO"),

            _ => null,
        };
}
