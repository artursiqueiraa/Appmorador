using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado PontoAcesso — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IPontoAcessoRepositorio
{
    /// <summary>Inclui a Propriedade (navegacao) — quem chama precisa dela para o check de ownership.</summary>
    Task<PontoAcesso?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PontoAcesso>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(PontoAcesso pontoAcesso, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
