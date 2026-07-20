namespace AppMorador.Application.Eventos;

/// <summary>
/// Filtro aplicado à Central de Eventos — deliberadamente agnóstico de fonte (nenhum
/// campo específico de central/zona), para que qualquer <see cref="IFonteEventos"/>
/// futura possa interpretá-lo sem depender de conceitos de uma fonte específica.
/// </summary>
public sealed class FiltroEventos
{
    /// <summary>Busca livre — hoje casa contra o texto do evento (ex.: nome de zona), não um campo estruturado.</summary>
    public string? Busca { get; init; }

    public DateTime? DesdeUtc { get; init; }

    public DateTime? AteUtc { get; init; }
}