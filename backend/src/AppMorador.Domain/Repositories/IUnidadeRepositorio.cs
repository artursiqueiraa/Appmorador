using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Unidade — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IUnidadeRepositorio
{
    /// <summary>Inclui a Propriedade (navegacao) — quem chama precisa dela para o check de ownership.</summary>
    Task<Unidade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Unidade>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(Unidade unidade, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
