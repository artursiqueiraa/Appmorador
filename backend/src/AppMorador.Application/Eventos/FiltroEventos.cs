using AppMorador.Domain.Entities;

namespace AppMorador.Application.Eventos;

/// <summary>
/// Filtro aplicado à Central de Eventos — deliberadamente agnóstico de fonte (nenhum
/// campo específico de central/zona), para que qualquer <see cref="IFonteEventos"/>
/// futura possa interpretá-lo sem depender de conceitos de uma fonte específica. Cada
/// fonte decide, por conta própria, se e como aplicar cada campo — um filtro que não
/// se aplica a uma fonte (ex.: <see cref="EquipamentoId"/> filtrando uma fonte que não
/// conhece esse conceito) faz a fonte devolver vazio, nunca ignorar o filtro
/// silenciosamente. Sprint 13 (ADR 0016) adicionou os 5 últimos campos.
/// </summary>
public sealed class FiltroEventos
{
    /// <summary>Busca livre — hoje casa contra o texto do evento (ex.: nome de zona), não um campo estruturado.</summary>
    public string? Busca { get; init; }

    public DateTime? DesdeUtc { get; init; }

    public DateTime? AteUtc { get; init; }

    /// <summary>Sprint 13 — restringe a um Equipamento específico (Control iD: direto pelo Id; JFL: resolvido via o auto-vínculo por Número de Série com a Central, ver ADR 0015).</summary>
    public Guid? EquipamentoId { get; init; }

    /// <summary>Sprint 13 — restringe ao fabricante do equipamento/central que originou o evento.</summary>
    public FabricanteEquipamento? Fabricante { get; init; }

    public OrigemEvento? Origem { get; init; }

    public CategoriaEvento? Categoria { get; init; }

    public SeveridadeEvento? Severidade { get; init; }
}