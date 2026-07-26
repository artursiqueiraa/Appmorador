using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class AutorizacaoRepositorio : IAutorizacaoRepositorio
{
    private readonly AppDbContext _db;

    public AutorizacaoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Autorizacao?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Autorizacoes
            .Include(a => a.MoradorResponsavel)
            .ThenInclude(m => m!.Unidade)
            .ThenInclude(u => u!.Propriedade)
            .Include(a => a.Unidade)
            .Include(a => a.Visitante)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    // Sem AsNoTracking em nenhum metodo abaixo: todos usados pelo cascade de exclusao
    // em algum nivel (Visitante/Unidade/Morador/Propriedade), alem de exibicao.
    public async Task<IReadOnlyList<Autorizacao>> ListByVisitanteAsync(Guid visitanteId, CancellationToken cancellationToken) =>
        await _db.Autorizacoes
            .Include(a => a.Unidade)
            .Include(a => a.MoradorResponsavel)
            .Where(a => a.VisitanteId == visitanteId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Autorizacao>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken) =>
        await _db.Autorizacoes
            .Where(a => a.UnidadeId == unidadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Autorizacao>> ListByMoradorResponsavelAsync(Guid moradorId, CancellationToken cancellationToken) =>
        await _db.Autorizacoes
            .Where(a => a.MoradorResponsavelId == moradorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Autorizacao>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Autorizacoes
            .Where(a => a.Unidade!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Autorizacao autorizacao, CancellationToken cancellationToken) =>
        await _db.Autorizacoes.AddAsync(autorizacao, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
