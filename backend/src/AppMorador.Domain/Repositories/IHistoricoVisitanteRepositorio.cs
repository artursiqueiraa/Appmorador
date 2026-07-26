using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IHistoricoVisitanteRepositorio
{
    Task AddAsync(HistoricoVisitante historico, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
