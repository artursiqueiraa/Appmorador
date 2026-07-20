namespace AppMorador.Application.Eventos;

/// <summary>
/// Porta de uma fonte de eventos para a Central de Eventos. Cada integração real
/// (central de alarme, controle de acesso, eventos de sistema) implementa esta
/// interface e traduz seu próprio dado interno para <see cref="EventoTimeline"/> —
/// quem consome esta porta nunca sabe qual entidade concreta está por trás.
/// Registrada no container como <c>IEnumerable&lt;IFonteEventos&gt;</c>: adicionar uma
/// fonte nova é só registrar mais uma implementação, sem alterar quem já consome.
/// </summary>
public interface IFonteEventos
{
    Task<(IReadOnlyList<EventoTimeline> Itens, int Total)> ConsultarEventosAsync(
        Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken);
}