using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

public interface IHistoricoVeiculoRepositorio
{
    Task AddAsync(HistoricoVeiculo historico, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
