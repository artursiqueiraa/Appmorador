using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IAuditoriaMasterRepositorio
{
    Task AddAsync(AuditoriaMaster registro, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditoriaMaster>> ListByUsuarioAsync(Guid usuarioId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditoriaMaster>> ListByPropriedadeAsync(Guid propriedadeId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken);

    /// <summary>Listagem geral, mais recentes primeiro — usada pela consulta de auditoria do Master sem filtro nenhum.</summary>
    Task<IReadOnlyList<AuditoriaMaster>> ListRecentesAsync(int quantidade, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
