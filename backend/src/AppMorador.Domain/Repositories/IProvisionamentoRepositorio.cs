using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IProvisionamentoRepositorio
{
    Task<Provisionamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Provisionamento>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(Provisionamento provisionamento, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
