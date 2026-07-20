namespace AppMorador.Application.Eventos;

/// <summary>
/// Forma interna unificada de um evento na Central de Eventos — nunca conhece a
/// entidade de origem (ex.: Ocorrencia) que a gerou, só o que <see cref="IFonteEventos"/>
/// já traduziu. Enriquecida deliberadamente para suportar fontes futuras
/// (controle de acesso, eventos de sistema) sem precisar mudar o formato.
/// </summary>
public sealed class EventoTimeline
{
    public required Guid Id { get; init; }

    public required OrigemEvento Origem { get; init; }

    public required CategoriaEvento Categoria { get; init; }

    public required SeveridadeEvento Severidade { get; init; }

    public required string Titulo { get; init; }

    public string? Descricao { get; init; }

    public required DateTime OcorridoEmUtc { get; init; }

    /// <summary>
    /// Espaço livre para cada fonte guardar dado extra próprio sem exigir mudança no
    /// formato comum. Aceita valores complexos/aninhados. Nenhuma fonte popula ainda —
    /// campo disponível para quando uma fonte real precisar.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Metadados { get; init; }
}