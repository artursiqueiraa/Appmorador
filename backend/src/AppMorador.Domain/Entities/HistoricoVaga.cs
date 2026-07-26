namespace AppMorador.Domain.Entities;

/// <summary>Auditoria pura (mesmo padrão de <see cref="HistoricoCredencial"/>) — nunca excluída, nem logicamente. Vaga é domínio independente de Veículo (nunca pertence a ele), por isso tem histórico próprio.</summary>
public class HistoricoVaga
{
    public Guid Id { get; set; }

    public Guid VagaId { get; set; }

    public Vaga? Vaga { get; set; }

    public required TipoEventoHistoricoVaga TipoEvento { get; set; }

    public required string Descricao { get; set; }

    public Guid? UsuarioId { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
