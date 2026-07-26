using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IHistoricoVagaRepositorio
{
    Task AddAsync(HistoricoVaga historico, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
