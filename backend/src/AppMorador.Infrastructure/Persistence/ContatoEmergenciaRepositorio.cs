using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class ContatoEmergenciaRepositorio : IContatoEmergenciaRepositorio
{
    private readonly AppDbContext _db;

    public ContatoEmergenciaRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<ContatoEmergencia?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.ContatosEmergencia.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ContatoEmergencia>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.ContatosEmergencia
            .Where(c => c.PropriedadeId == propriedadeId)
            .OrderBy(c => c.Ordem)
            .ThenBy(c => c.CriadoEmUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(ContatoEmergencia contato, CancellationToken cancellationToken) =>
        await _db.ContatosEmergencia.AddAsync(contato, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
