using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class VisitanteRepositorio : IVisitanteRepositorio
{
    private readonly AppDbContext _db;

    public VisitanteRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Visitante?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Visitantes
            .Include(v => v.Propriedade)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    // Sem AsNoTracking: usado tanto para exibicao quanto pelo cascade de exclusao da
    // Propriedade, que precisa rastrear e salvar as mudancas.
    public async Task<IReadOnlyList<Visitante>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Visitantes
            .Where(v => v.PropriedadeId == propriedadeId)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Visitante visitante, CancellationToken cancellationToken) =>
        await _db.Visitantes.AddAsync(visitante, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
