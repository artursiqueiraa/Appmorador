using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class MoradorRepositorio : IMoradorRepositorio
{
    private readonly AppDbContext _db;

    public MoradorRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Morador?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Moradores
            .Include(m => m.Unidade)
            .ThenInclude(u => u!.Propriedade)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    // Sem AsNoTracking: usado tanto para exibicao quanto para o cascade de exclusao
    // logica (UnidadeServico.DeleteAsync precisa rastrear e salvar as mudancas).
    public async Task<IReadOnlyList<Morador>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken) =>
        await _db.Moradores
            .Where(m => m.UnidadeId == unidadeId)
            .OrderBy(m => m.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Morador>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Moradores
            .Where(m => m.Unidade!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.Moradores.CountAsync(m => m.Unidade!.PropriedadeId == propriedadeId, cancellationToken);

    public async Task AddAsync(Morador morador, CancellationToken cancellationToken) =>
        await _db.Moradores.AddAsync(morador, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
