using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class VagaRepositorio : IVagaRepositorio
{
    private readonly AppDbContext _db;

    public VagaRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Vaga?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Vagas
            .Include(v => v.Propriedade)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    // Sem AsNoTracking: usado tanto para exibicao/Dashboard quanto pelo cascade de
    // exclusao da Propriedade, que precisa rastrear e salvar as mudancas.
    public async Task<IReadOnlyList<Vaga>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Vagas
            .Where(v => v.PropriedadeId == propriedadeId)
            .OrderBy(v => v.Numero)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.Vagas.CountAsync(v => v.PropriedadeId == propriedadeId, cancellationToken);

    public async Task AddAsync(Vaga vaga, CancellationToken cancellationToken) =>
        await _db.Vagas.AddAsync(vaga, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
