using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class HistoricoEntregaRepositorio : IHistoricoEntregaRepositorio
{
    private readonly AppDbContext _db;

    public HistoricoEntregaRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(HistoricoEntrega historico, CancellationToken cancellationToken) =>
        await _db.HistoricoEntregas.AddAsync(historico, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
