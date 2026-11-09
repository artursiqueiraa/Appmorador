using Microsoft.Extensions.Logging;

namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023) — implementação da decisão de negócio "notificar ou não,
/// com qual mensagem". A tabela de mensagens (Fase 4 da missão) é fixa aqui, não
/// configurável — mudar o texto de uma notificação é uma decisão de produto, não
/// uma configuração de runtime.
///
/// Sprint 24 (ADR 0040) — quando o contexto traz um <see cref="ContextoNotificacao.AlertaId"/>,
/// a notificação é crítica (canal próprio, abre a tela "Estou ciente") e o debounce passa a ser
/// por alerta: dois alertas diferentes nunca se suprimem.
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

    public Task NotificarAsync(EventoNotificacaoTipo tipo, ContextoNotificacao contexto, CancellationToken cancellationToken) =>
        NotificarInternoAsync(tipo, contexto, usuarioIds: null, cancellationToken);

    public Task NotificarUsuariosAsync(
        EventoNotificacaoTipo tipo, ContextoNotificacao contexto, IReadOnlyCollection<Guid> usuarioIds, CancellationToken cancellationToken) =>
        usuarioIds.Count == 0 ? Task.CompletedTask : NotificarInternoAsync(tipo, contexto, usuarioIds, cancellationToken);

    private async Task NotificarInternoAsync(
        EventoNotificacaoTipo tipo, ContextoNotificacao contexto, IReadOnlyCollection<Guid>? usuarioIds, CancellationToken cancellationToken)
    {
        // Sprint 19 (Fase 8.2) — debounce por (tipo, alerta OU equipamento OU propriedade): um
        // equipamento oscilando offline/online repetidamente, ou um comando acionado
        // várias vezes seguidas, gera só a primeira notificação por minuto.
        var chaveDebounce = contexto.AlertaId ?? contexto.EquipamentoId ?? contexto.PropriedadeId;
        if (!await _debounce.PodeNotificarAsync(tipo, chaveDebounce, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Notificacao {Tipo} suprimida por debounce (chave {Chave})", tipo, chaveDebounce);
            return;
        }

        var mensagem = MontarMensagem(tipo, contexto);
        if (mensagem is null)
        {
            return;
        }

        var critica = contexto.AlertaId is not null;
        var payload = new NotificacaoPayload
        {
            Titulo = mensagem.Value.Titulo,
            Corpo = mensagem.Value.Corpo,
            EventoTipo = tipo,
            PropriedadeId = contexto.PropriedadeId,
            EquipamentoId = contexto.EquipamentoId,
            AlertaId = contexto.AlertaId,
            Acao = critica ? "ABRIR_ALERTA" : mensagem.Value.Acao,
            Prioridade = critica ? PrioridadeNotificacao.Critica : mensagem.Value.Prioridade,
        };

        if (usuarioIds is null)
        {
            await _notificationService.EnviarParaPropriedadeAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _notificationService.EnviarParaUsuariosAsync(payload, usuarioIds, cancellationToken).ConfigureAwait(false);
        }

        await _debounce.RegistrarEnvioAsync(tipo, chaveDebounce, cancellationToken).ConfigureAwait(false);
    }

    private static string Onde(ContextoNotificacao contexto, string padrao) =>
        contexto.Descricao is null ? padrao : $"{contexto.Descricao} em {contexto.NomePropriedade}";

    private static (string Titulo, string Corpo, PrioridadeNotificacao Prioridade, string Acao)? MontarMensagem(EventoNotificacaoTipo tipo, ContextoNotificacao contexto) =>
        tipo switch
        {
            EventoNotificacaoTipo.AlarmeDisparado => (
                "⚠️ Alarme disparado",
                Onde(contexto, $"Uma área protegida foi acionada em {contexto.NomePropriedade}"),
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

            EventoNotificacaoTipo.PanicoAcionado => (
                "🆘 Pânico acionado",
                Onde(contexto, $"Um pedido de socorro foi acionado em {contexto.NomePropriedade}"),
                PrioridadeNotificacao.Critica,
                "ABRIR_ALERTA"),

            EventoNotificacaoTipo.ViolacaoDetectada => (
                "⚠️ Tentativa de violação",
                Onde(contexto, $"Alguém tentou violar o alarme em {contexto.NomePropriedade}"),
                PrioridadeNotificacao.Critica,
                "ABRIR_ALERTA"),

            EventoNotificacaoTipo.CentralSemComunicacao => (
                "📡 Central sem comunicação",
                $"A central de alarme de {contexto.NomePropriedade} parou de responder. Verifique o local.",
                PrioridadeNotificacao.Critica,
                "ABRIR_ALERTA"),

            EventoNotificacaoTipo.FalhaEnergia => (
                "🔌 Falta de energia",
                Onde(contexto, $"A central de {contexto.NomePropriedade} está funcionando pela bateria."),
                PrioridadeNotificacao.Alta,
                "ABRIR_APP_HISTORICO"),

            EventoNotificacaoTipo.BateriaBaixa => (
                "🔋 Bateria fraca",
                Onde(contexto, $"Uma bateria do alarme está fraca em {contexto.NomePropriedade}") + ". Vale agendar a troca.",
                PrioridadeNotificacao.Normal,
                "ABRIR_APP_HISTORICO"),

            EventoNotificacaoTipo.AlertaEscalonado => (
                "🚨 Alerta sem resposta",
                $"{contexto.Descricao ?? "Um alerta"} em {contexto.NomePropriedade} ainda não foi confirmado por ninguém.",
                PrioridadeNotificacao.Critica,
                "ABRIR_ALERTA"),

            _ => null,
        };
}
