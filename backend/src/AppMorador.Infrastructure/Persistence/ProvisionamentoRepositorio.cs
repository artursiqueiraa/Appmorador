using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class ProvisionamentoRepositorio : IProvisionamentoRepositorio
{
    private readonly AppDbContext _db;

    public ProvisionamentoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Provisionamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Provisionamentos.Include(p => p.Propriedade).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Provisionamento>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Provisionamentos
            .Where(p => p.PropriedadeId == propriedadeId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Provisionamento provisionamento, CancellationToken cancellationToken) =>
        await _db.Provisionamentos.AddAsync(provisionamento, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
