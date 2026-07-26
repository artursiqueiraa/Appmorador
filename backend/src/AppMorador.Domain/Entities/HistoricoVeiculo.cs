namespace AppMorador.Domain.Entities;

/// <summary>Auditoria pura (mesmo padrão de <see cref="HistoricoCredencial"/>) — nunca excluída, nem logicamente.</summary>
public class HistoricoVeiculo
{
    public Guid Id { get; set; }

    public Guid VeiculoId { get; set; }

    public Veiculo? Veiculo { get; set; }

    public required TipoEventoHistoricoVeiculo TipoEvento { get; set; }

    public required string Descricao { get; set; }

    public Guid? UsuarioId { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
