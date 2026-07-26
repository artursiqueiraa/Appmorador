using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class EntregaRepositorio : IEntregaRepositorio
{
    private readonly AppDbContext _db;

    public EntregaRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Entrega?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Entregas
            .Include(e => e.MoradorDestinatario)
            .ThenInclude(m => m!.Unidade)
            .ThenInclude(u => u!.Propriedade)
            .Include(e => e.Unidade)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    // Sem AsNoTracking em nenhum metodo abaixo: usados tanto para exibicao/Dashboard
    // quanto pelo cascade de exclusao (Morador/Unidade/Propriedade), que precisa
    // rastrear e salvar as mudancas.
    public async Task<IReadOnlyList<Entrega>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken) =>
        await _db.Entregas
            .Where(e => e.MoradorDestinatarioId == moradorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Entrega>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken) =>
        await _db.Entregas
            .Where(e => e.UnidadeId == unidadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Entrega>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Entregas
            .Include(e => e.MoradorDestinatario)
            .Include(e => e.Unidade)
            .Where(e => e.Unidade!.PropriedadeId == propriedadeId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusEntrega? status, CancellationToken cancellationToken)
    {
        var query = _db.Entregas.Where(e => e.Unidade!.PropriedadeId == propriedadeId);
        if (status is not null)
        {
            query = query.Where(e => e.Status == status);
        }

        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Entrega entrega, CancellationToken cancellationToken) =>
        await _db.Entregas.AddAsync(entrega, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
