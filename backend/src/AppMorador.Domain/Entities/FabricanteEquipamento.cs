namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 11 — fabricante do equipamento de integração. Cada valor corresponde a um
/// futuro Provider próprio (<see cref="Equipamento"/> nunca conhece o protocolo do
/// fabricante) — hoje só ControlId tem Provider real implementado.
/// </summary>
public enum FabricanteEquipamento
{
    ControlId,
    Jfl,
    Intelbras,
    Hikvision,
    Dahua,
    Outro,
}
