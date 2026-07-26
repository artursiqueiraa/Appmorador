namespace AppMorador.Domain.Entities;

/// <summary>Auditoria pura (mesmo padrão de <see cref="HistoricoCredencial"/>) — nunca excluída, nem logicamente.</summary>
public class HistoricoEntrega
{
    public Guid Id { get; set; }

    public Guid EntregaId { get; set; }

    public Entrega? Entrega { get; set; }

    public required TipoEventoHistoricoEntrega TipoEvento { get; set; }

    public required string Descricao { get; set; }

    public Guid? UsuarioId { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
