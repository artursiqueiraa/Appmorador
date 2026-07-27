namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0027) — Fabricante e Modelo são conceitos deliberadamente
/// separados: um mesmo <see cref="FabricanteEquipamento"/> (ex.: ControlId) pode ter
/// vários modelos com capacidades diferentes entre si (ex.: um leitor só com TAG,
/// outro com Face+TAG+QrCode). Capacidades pertencem ao Modelo, nunca ao Fabricante.
/// Substitui <c>Equipamento.Modelo</c> (antes um texto livre) — ver ADR 0027 para o
/// racional da migração e a estratégia de preservar os dados já existentes.
/// </summary>
public class ModeloEquipamento
{
    public Guid Id { get; set; }

    public required FabricanteEquipamento Fabricante { get; set; }

    public required string Nome { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
