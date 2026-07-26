using AppMorador.Application.Common;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Eventos;

/// <summary>
/// Orquestra a Central de Eventos: confirma posse da propriedade (mesmo padrão de
/// <see cref="AppMorador.Application.Dashboard.DashboardServico"/>) e delega às fontes
/// registradas. Nunca conhece qual entidade concreta cada fonte consulta por dentro.
/// </summary>
public sealed class EventosServico : IEventosServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IEnumerable<IFonteEventos> _fontes;

    public EventosServico(IPropriedadeRepositorio propriedades, IEnumerable<IFonteEventos> fontes)
    {
        _propriedades = propriedades;
        _fontes = fontes;
    }

    public async Task<Result<EventosPaginadosResponse>> GetEventosAsync(
        Guid proprietarioId, Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<EventosPaginadosResponse>.Fail("Propriedade não encontrada.");
        }

        // Sprint 11 — duas fontes reais agora (JflFonteEventos + EquipamentoFonteEventos):
        // cada fonte devolve seu proprio top-N (N = pagina*tamanhoPagina, sempre ordenado
        // desc por data), o suficiente para garantir que a pagina pedida do merge esteja
        // completa. Custo cresce com o numero da pagina — aceitavel na escala atual do
        // projeto; registrado como limitacao conhecida em DIVIDA_TECNICA.md caso o volume
        // de eventos cresça. Consultado sequencialmente (nunca Task.WhenAll): as fontes
        // compartilham a mesma instancia (Scoped) de AppDbContext, que nao e thread-safe
        // para operacoes concorrentes.
        var resultadosPorFonte = new List<(IReadOnlyList<EventoTimeline> Itens, int Total)>();
        foreach (var fonte in _fontes)
        {
            resultadosPorFonte.Add(
                await fonte.ConsultarEventosAsync(propriedadeId, filtro, 1, pagina * tamanhoPagina, cancellationToken).ConfigureAwait(false));
        }

        var itensTimeline = resultadosPorFonte
            .SelectMany(r => r.Itens)
            .OrderByDescending(e => e.OcorridoEmUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToList();
        var total = resultadosPorFonte.Sum(r => r.Total);

        var itens = itensTimeline.Select(ToResponse).ToList();
        var totalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanhoPagina);

        return Result<EventosPaginadosResponse>.Ok(new EventosPaginadosResponse
        {
            Itens = itens,
            PaginaAtual = pagina,
            TotalPaginas = totalPaginas,
            TotalItens = total,
        });
    }

    private static EventoResponse ToResponse(EventoTimeline evento) => new()
    {
        Id = evento.Id,
        Titulo = evento.Titulo,
        Descricao = evento.Descricao,
        OcorridoEmUtc = evento.OcorridoEmUtc,
        Destaque = evento.Severidade == SeveridadeEvento.Critico,
    };
}