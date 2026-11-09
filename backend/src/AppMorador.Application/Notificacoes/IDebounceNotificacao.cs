namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023, Fase 8.2) — limita a 1 notificação por minuto para o mesmo
/// (tipo de evento, chave).
///
/// Sprint 24 (ADR 0040) — passou a ser persistido no banco (tabela ControlesEnvioNotificacao,
/// fecha a dívida 41): reiniciar o servidor não reenvia o que acabou de ser enviado. Por isso a
/// interface virou assíncrona.
/// </summary>
public interface IDebounceNotificacao
{
    Task<bool> PodeNotificarAsync(EventoNotificacaoTipo tipo, Guid chave, CancellationToken cancellationToken);

    Task RegistrarEnvioAsync(EventoNotificacaoTipo tipo, Guid chave, CancellationToken cancellationToken);
}
