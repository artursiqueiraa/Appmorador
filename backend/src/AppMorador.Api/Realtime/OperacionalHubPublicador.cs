using System.Collections.Concurrent;
using AppMorador.Application.Eventos;
using AppMorador.Application.Operacional;
using Microsoft.AspNetCore.SignalR;

namespace AppMorador.Api.Realtime;

/// <summary>
/// Sprint 14 (ADR 0017) — única implementação real de
/// <see cref="IOperacionalEventoPublicador"/>. Nunca calcula/classifica nada: recebe
/// o DTO já pronto (gerado por <see cref="SnapshotOperacionalServico"/> ou
/// <see cref="EventosServico"/>) e só transporta via SignalR para o grupo da
/// Propriedade. Falha de entrega em tempo real nunca pode propagar para o fluxo de
/// domínio que originou a publicação — o dado já foi persistido com sucesso antes
/// desta chamada, então qualquer erro aqui só é logado (best-effort).
///
/// Debounce (item 8 do escopo): protege só contra o caso concreto que existe hoje —
/// uma ação de comando JFL que consulta+substitui status em sequência rápida (ex.:
/// inibir zona) podendo gerar 2 publicações quase simultâneas para a mesma
/// Propriedade. Um <see cref="ConcurrentDictionary{TKey,TValue}"/> em memória (não uma
/// fila/worker — mesma régua de simplicidade de MVP já usada em outras Sprints) é
/// suficiente: descarta uma publicação se outra idêntica em motivo já saiu há menos
/// de <see cref="JanelaDebounce"/> para a mesma Propriedade.
/// </summary>
public sealed class OperacionalHubPublicador : IOperacionalEventoPublicador
{
    private static readonly TimeSpan JanelaDebounce = TimeSpan.FromMilliseconds(750);

    private readonly IHubContext<OperacionalHub> _hub;
    private readonly ILogger<OperacionalHubPublicador> _logger;
    private readonly ConcurrentDictionary<(Guid PropriedadeId, MotivoAtualizacaoOperacional Motivo), DateTime> _ultimaPublicacao = new();

    public OperacionalHubPublicador(IHubContext<OperacionalHub> hub, ILogger<OperacionalHubPublicador> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublicarSnapshotAsync(
        Guid propriedadeId, SnapshotOperacionalResponse snapshot, MotivoAtualizacaoOperacional motivo, CancellationToken cancellationToken)
    {
        if (DeveDescartarPorDebounce(propriedadeId, motivo))
        {
            return;
        }

        try
        {
            await _hub.Clients.Group(OperacionalHub.GrupoPropriedade(propriedadeId))
                .SendAsync("OperacionalAtualizado", new { propriedadeId, snapshot, motivo = motivo.ToString() }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Falha ao publicar atualizacao operacional em tempo real (propriedade {PropriedadeId}, motivo {Motivo})",
                propriedadeId, motivo);
        }
    }

    public async Task PublicarNovoEventoAsync(Guid propriedadeId, EventoResponse evento, CancellationToken cancellationToken)
    {
        try
        {
            await _hub.Clients.Group(OperacionalHub.GrupoPropriedade(propriedadeId))
                .SendAsync("NovoEventoOperacional", new { propriedadeId, evento }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar novo evento em tempo real (propriedade {PropriedadeId})", propriedadeId);
        }
    }

    private bool DeveDescartarPorDebounce(Guid propriedadeId, MotivoAtualizacaoOperacional motivo)
    {
        var chave = (propriedadeId, motivo);
        var agora = DateTime.UtcNow;

        var descarta = _ultimaPublicacao.TryGetValue(chave, out var ultima) && agora - ultima < JanelaDebounce;
        _ultimaPublicacao[chave] = agora;
        return descarta;
    }
}
