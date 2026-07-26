using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Vaga — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IVagaRepositorio
{
    /// <summary>Inclui a Propriedade (navegacao) — quem chama precisa dela para o check de ownership.</summary>
    Task<Vaga?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Usado tanto para exibicao/Dashboard quanto pelo cascade de exclusao quando a Propriedade e excluida.</summary>
    Task<IReadOnlyList<Vaga>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(Vaga vaga, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
