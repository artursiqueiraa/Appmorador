using System.Collections.Concurrent;
using AppMorador.Application.Notificacoes;

namespace AppMorador.Infrastructure.Notifications;

/// <summary>
/// Sprint 19 (ADR 0023, Fase 8.2) — implementação em memória, registrada como
/// Singleton (precisa sobreviver entre requests para o debounce funcionar de
/// verdade). Ver <see cref="IDebounceNotificacao"/> para o racional de não usar
/// Redis/fila nesta Sprint.
/// </summary>
public sealed class DebounceNotificacaoEmMemoria : IDebounceNotificacao
{
    private static readonly TimeSpan Janela = TimeSpan.FromSeconds(60);
    private readonly ConcurrentDictionary<(EventoNotificacaoTipo Tipo, Guid Chave), DateTime> _ultimoEnvio = new();

    public bool PodeNotificar(EventoNotificacaoTipo tipo, Guid chave)
    {
        if (_ultimoEnvio.TryGetValue((tipo, chave), out var ultimoEnvioUtc))
        {
            return DateTime.UtcNow - ultimoEnvioUtc >= Janela;
        }

        return true;
    }

    public void RegistrarEnvio(EventoNotificacaoTipo tipo, Guid chave) => _ultimoEnvio[(tipo, chave)] = DateTime.UtcNow;
}
