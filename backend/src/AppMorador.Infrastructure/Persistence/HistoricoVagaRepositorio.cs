using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class HistoricoVagaRepositorio : IHistoricoVagaRepositorio
{
    private readonly AppDbContext _db;

    public HistoricoVagaRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(HistoricoVaga historico, CancellationToken cancellationToken) =>
        await _db.HistoricoVagas.AddAsync(historico, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
