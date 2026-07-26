using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o snapshot operacional (1:1 com Propriedade) — implementação (EF Core) vive em Infrastructure.</summary>
public interface ISnapshotOperacionalRepositorio
{
    Task<SnapshotOperacional?> GetByPropriedadeIdAsync(Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Insere ou substitui o snapshot existente da Propriedade (upsert, sempre 1:1).</summary>
    Task UpsertAsync(SnapshotOperacional snapshot, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
