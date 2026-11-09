namespace AppMorador.Domain.Entities;

public enum TipoEventoConectividade
{
    Perdida,
    Restabelecida,
}

/// <summary>
/// Sprint 24 (ADR 0040) — histórico de quando uma central perdeu e recuperou a comunicação com o
/// servidor. Fecha a dívida 24 (Online/Offline não virava evento). Auditoria pura: sem soft delete.
/// </summary>
public class EventoConectividade
{
    public Guid Id { get; set; }

    public Guid EquipamentoId { get; set; }

    public Equipamento? Equipamento { get; set; }

    public Guid PropriedadeId { get; set; }

    public TipoEventoConectividade Tipo { get; set; }

    public DateTime OcorridoEmUtc { get; set; }
}
