using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class HistoricoVisitanteRepositorio : IHistoricoVisitanteRepositorio
{
    private readonly AppDbContext _db;

    public HistoricoVisitanteRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(HistoricoVisitante historico, CancellationToken cancellationToken) =>
        await _db.HistoricoVisitantes.AddAsync(historico, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
