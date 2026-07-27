namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023, Fase 8.2) — limita a 1 notificação por minuto para o mesmo
/// (tipo de evento, chave). Implementação em memória (sem Redis/fila) — mesmo
/// racional de simplicidade já aplicado a todo MVP deste projeto: o backend roda
/// numa única instância hoje, um dicionário em memória resolve o problema real sem
/// introduzir infraestrutura nova. Revisar se o backend algum dia rodar em mais de
/// uma instância.
/// </summary>
public interface IDebounceNotificacao
{
    bool PodeNotificar(EventoNotificacaoTipo tipo, Guid chave);

    void RegistrarEnvio(EventoNotificacaoTipo tipo, Guid chave);
}
