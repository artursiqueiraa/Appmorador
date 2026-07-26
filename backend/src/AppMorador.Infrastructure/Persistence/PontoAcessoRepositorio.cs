using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class PontoAcessoRepositorio : IPontoAcessoRepositorio
{
    private readonly AppDbContext _db;

    public PontoAcessoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<PontoAcesso?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.PontosAcesso.Include(p => p.Propriedade).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    // Sem AsNoTracking: usado tanto para exibicao quanto para o cascade de exclusao
    // (PropriedadeServico.DeleteAsync precisa rastrear e salvar as mudancas).
    public async Task<IReadOnlyList<PontoAcesso>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.PontosAcesso
            .Where(p => p.PropriedadeId == propriedadeId)
            .OrderBy(p => p.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.PontosAcesso.CountAsync(p => p.PropriedadeId == propriedadeId, cancellationToken);

    public async Task AddAsync(PontoAcesso pontoAcesso, CancellationToken cancellationToken) =>
        await _db.PontosAcesso.AddAsync(pontoAcesso, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
