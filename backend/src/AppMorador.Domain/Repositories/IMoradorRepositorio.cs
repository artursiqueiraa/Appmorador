using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Morador — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IMoradorRepositorio
{
    /// <summary>Inclui Unidade e Propriedade (navegacao) — quem chama precisa delas para o check de ownership.</summary>
    Task<Morador?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Morador>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken);

    /// <summary>Todos os moradores de todas as unidades de uma propriedade — usado pelo cascade de exclusao.</summary>
    Task<IReadOnlyList<Morador>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(Morador morador, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
