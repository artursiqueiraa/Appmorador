using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class HistoricoVeiculoRepositorio : IHistoricoVeiculoRepositorio
{
    private readonly AppDbContext _db;

    public HistoricoVeiculoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(HistoricoVeiculo historico, CancellationToken cancellationToken) =>
        await _db.HistoricoVeiculos.AddAsync(historico, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
