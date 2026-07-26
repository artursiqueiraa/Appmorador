using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IHistoricoEntregaRepositorio
{
    Task AddAsync(HistoricoEntrega historico, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
