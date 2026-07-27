namespace AppMorador.Domain.Entities;

/// <summary>Vínculo NxN entre <see cref="ModeloEquipamento"/> e <see cref="EquipamentoCapacidade"/> — a presença de uma linha significa "este modelo suporta".</summary>
public class ModeloEquipamentoCapacidade
{
    public Guid Id { get; set; }

    public Guid ModeloEquipamentoId { get; set; }

    public ModeloEquipamento? ModeloEquipamento { get; set; }

    public EquipamentoCapacidade Capacidade { get; set; }
}
