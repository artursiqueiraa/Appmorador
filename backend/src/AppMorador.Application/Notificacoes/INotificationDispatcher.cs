namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023) — ponto de entrada único para qualquer Application Service
/// que precise notificar algo. Decide (Regra de Ouro: na dúvida, notificar),
/// aplica o debounce de 60s, monta a mensagem amigável e delega o envio a
/// <see cref="INotificationService"/> — nunca fala com <see cref="INotificationProvider"/>
/// diretamente.
/// </summary>
public interface INotificationDispatcher
{
    Task NotificarAsync(EventoNotificacaoTipo tipo, ContextoNotificacao contexto, CancellationToken cancellationToken);
}
