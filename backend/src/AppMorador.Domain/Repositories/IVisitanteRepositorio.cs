using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Visitante — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IVisitanteRepositorio
{
    /// <summary>Inclui Propriedade (navegacao) — quem chama precisa dela para o check de ownership.</summary>
    Task<Visitante?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Usado tanto para exibicao quanto pelo cascade de exclusao quando a Propriedade e excluida.</summary>
    Task<IReadOnlyList<Visitante>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(Visitante visitante, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
