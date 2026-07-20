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

        // Hoje existe uma única fonte registrada (JflFonteEventos). Agregar/paginar entre
        // múltiplas fontes é lógica que não existe ainda porque não há uma segunda fonte
        // real para validar contra — fica para quando essa fonte existir de fato.
        var fonte = _fontes.First();
        var (itensTimeline, total) = await fonte
            .ConsultarEventosAsync(propriedadeId, filtro, pagina, tamanhoPagina, cancellationToken)
            .ConfigureAwait(false);

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