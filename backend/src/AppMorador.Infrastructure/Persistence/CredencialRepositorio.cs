using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class CredencialRepositorio : ICredencialRepositorio
{
    private readonly AppDbContext _db;

    public CredencialRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Credencial?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Credenciais
            .Include(c => c.Morador)
            .ThenInclude(m => m!.Unidade)
            .ThenInclude(u => u!.Propriedade)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    // Sem AsNoTracking em nenhum metodo abaixo: ListByMoradorAsync tambem e usado
    // pelo cascade de exclusao (MoradorServico.DeleteAsync), e os outros dois pelo
    // cascade de Unidade/Propriedade — todos precisam rastrear e salvar as mudancas.
    public async Task<IReadOnlyList<Credencial>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken) =>
        await _db.Credenciais
            .Where(c => c.MoradorId == moradorId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Credencial>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken) =>
        await _db.Credenciais
            .Where(c => c.Morador!.UnidadeId == unidadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Credencial>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Credenciais
            .Where(c => c.Morador!.Unidade!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusCredencial? status, CancellationToken cancellationToken)
    {
        var query = _db.Credenciais.Where(c => c.Morador!.Unidade!.PropriedadeId == propriedadeId);
        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Credencial credencial, CancellationToken cancellationToken) =>
        await _db.Credenciais.AddAsync(credencial, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
