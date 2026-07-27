namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023) — resolve QUEM recebe (dispositivos ativos do dono da
/// Propriedade, filtrados pelo canal habilitado em cada dispositivo) e delega o
/// envio ao <see cref="INotificationProvider"/>. Nunca decide SE deve notificar
/// nem monta o texto — isso é responsabilidade do <see cref="INotificationDispatcher"/>.
/// </summary>
public interface INotificationService
{
    Task EnviarParaPropriedadeAsync(NotificacaoPayload payload, CancellationToken cancellationToken);
}
