using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Propriedade — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IPropriedadeRepositorio
{
    Task<Propriedade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Propriedade>> ListByOwnerAsync(Guid proprietarioId, CancellationToken cancellationToken);

    /// <summary>Sprint 22A (ADR 0029) — total de propriedades por Tipo, para o Dashboard Operacional.</summary>
    Task<IReadOnlyDictionary<TipoPropriedade, int>> ContarPorTipoAsync(CancellationToken cancellationToken);

    /// <summary>Sprint 22A (ADR 0029) — quantidade de propriedades por dono, para a listagem de clientes (coluna "Propriedades").</summary>
    Task<IReadOnlyDictionary<Guid, int>> ContarPorProprietariosAsync(IReadOnlyCollection<Guid> proprietarioIds, CancellationToken cancellationToken);

    Task AddAsync(Propriedade propriedade, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
