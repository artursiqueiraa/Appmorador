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
    /// <summary>Envia para o dono da propriedade.</summary>
    Task NotificarAsync(EventoNotificacaoTipo tipo, ContextoNotificacao contexto, CancellationToken cancellationToken);

    /// <summary>Sprint 24 (ADR 0040) — envia para usuários específicos (escalonamento para membros).</summary>
    Task NotificarUsuariosAsync(
        EventoNotificacaoTipo tipo, ContextoNotificacao contexto, IReadOnlyCollection<Guid> usuarioIds, CancellationToken cancellationToken);
}
