namespace AppMorador.Domain.Entities;

/// <summary>
/// Auditoria pura (mesmo padrão de <see cref="HistoricoCredencial"/>) — nunca excluída,
/// nem logicamente. <see cref="AutorizacaoId"/> é nulo só no evento VisitanteRemovido
/// (não amarrado a uma autorização específica); todo o resto sempre tem os dois.
/// </summary>
public class HistoricoVisitante
{
    public Guid Id { get; set; }

    public Guid VisitanteId { get; set; }

    public Visitante? Visitante { get; set; }

    public Guid? AutorizacaoId { get; set; }

    public Autorizacao? Autorizacao { get; set; }

    public required TipoEventoHistoricoVisitante TipoEvento { get; set; }

    public required string Descricao { get; set; }

    public Guid? UsuarioId { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
