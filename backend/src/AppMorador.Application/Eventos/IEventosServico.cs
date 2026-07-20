using AppMorador.Application.Common;

namespace AppMorador.Application.Eventos;

public interface IEventosServico
{
    Task<Result<EventosPaginadosResponse>> GetEventosAsync(
        Guid proprietarioId, Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken);
}