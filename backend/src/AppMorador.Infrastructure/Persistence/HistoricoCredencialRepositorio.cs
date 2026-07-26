using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class HistoricoCredencialRepositorio : IHistoricoCredencialRepositorio
{
    private readonly AppDbContext _db;

    public HistoricoCredencialRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(HistoricoCredencial historico, CancellationToken cancellationToken) =>
        await _db.HistoricoCredenciais.AddAsync(historico, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
