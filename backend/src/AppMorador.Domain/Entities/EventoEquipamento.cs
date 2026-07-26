namespace AppMorador.Domain.Entities;

/// <summary>
/// Um evento importado de um equipamento (ex.: log de acesso do Control iD) — mesmo
/// espírito de <see cref="Ocorrencia"/> (JFL): dado bruto de auditoria, nunca excluído,
/// sem soft delete/query filter. Alimenta a Central de Eventos já existente através de
/// um <c>IFonteEventos</c> próprio em Infrastructure — nunca uma estrutura paralela de
/// "eventos de equipamento".
/// </summary>
public class EventoEquipamento
{
    public Guid Id { get; set; }

    public Guid EquipamentoId { get; set; }

    public Equipamento? Equipamento { get; set; }

    /// <summary>Código/identificador bruto do evento como o fabricante o expõe (ex.: tipo de log do Control iD) — nunca mostrado cru ao usuário final.</summary>
    public required string CodigoEventoOriginal { get; set; }

    public required string Descricao { get; set; }

    public required DateTime OcorridoEmUtc { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
