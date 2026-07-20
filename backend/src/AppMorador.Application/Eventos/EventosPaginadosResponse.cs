namespace AppMorador.Application.Eventos;

public sealed class EventosPaginadosResponse
{
    public required IReadOnlyList<EventoResponse> Itens { get; init; }

    public required int PaginaAtual { get; init; }

    public required int TotalPaginas { get; init; }

    public required int TotalItens { get; init; }
}