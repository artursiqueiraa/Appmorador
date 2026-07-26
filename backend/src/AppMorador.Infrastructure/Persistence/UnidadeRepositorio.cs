using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class UnidadeRepositorio : IUnidadeRepositorio
{
    private readonly AppDbContext _db;

    public UnidadeRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Unidade?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Unidades.Include(u => u.Propriedade).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    // Sem AsNoTracking: usado tanto para exibicao quanto para o cascade de exclusao
    // logica (PropriedadeServico.DeleteAsync precisa rastrear e salvar as mudancas).
    public async Task<IReadOnlyList<Unidade>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Unidades
            .Where(u => u.PropriedadeId == propriedadeId)
            .OrderBy(u => u.Identificacao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.Unidades.CountAsync(u => u.PropriedadeId == propriedadeId, cancellationToken);

    public async Task AddAsync(Unidade unidade, CancellationToken cancellationToken) =>
        await _db.Unidades.AddAsync(unidade, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
