using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class DispositivoPushRepositorio : IDispositivoPushRepositorio
{
    private readonly AppDbContext _db;

    public DispositivoPushRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<DispositivoPush?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.DispositivosPush.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<DispositivoPush?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        _db.DispositivosPush.FirstOrDefaultAsync(d => d.Token == token, cancellationToken);

    public async Task<IReadOnlyList<DispositivoPush>> ListAtivosByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        await _db.DispositivosPush
            .Where(d => d.UsuarioId == usuarioId && d.Ativo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(DispositivoPush dispositivo, CancellationToken cancellationToken) =>
        await _db.DispositivosPush.AddAsync(dispositivo, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
