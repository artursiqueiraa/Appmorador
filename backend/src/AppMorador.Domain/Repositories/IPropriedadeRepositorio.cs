using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Propriedade — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IPropriedadeRepositorio
{
    Task<Propriedade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Propriedade>> ListByOwnerAsync(Guid proprietarioId, CancellationToken cancellationToken);

    Task AddAsync(Propriedade propriedade, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
