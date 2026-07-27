using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class EquipamentoRepositorio : IEquipamentoRepositorio
{
    private readonly AppDbContext _db;

    public EquipamentoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<Equipamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Equipamentos
            .Include(e => e.Propriedade)
            .Include(e => e.ModeloEquipamento)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Equipamento>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.Equipamentos
            .Include(e => e.ModeloEquipamento)
            .Where(e => e.PropriedadeId == propriedadeId)
            .OrderBy(e => e.Nome)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusEquipamento? status, CancellationToken cancellationToken)
    {
        var query = _db.Equipamentos.Where(e => e.PropriedadeId == propriedadeId);
        if (status is not null)
        {
            query = query.Where(e => e.Status == status);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<DateTime?> GetUltimaSincronizacaoAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.Equipamentos
            .Where(e => e.PropriedadeId == propriedadeId && e.UltimaSincronizacaoUtc != null)
            .OrderByDescending(e => e.UltimaSincronizacaoUtc)
            .Select(e => (DateTime?)e.UltimaSincronizacaoUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Equipamento equipamento, CancellationToken cancellationToken) =>
        await _db.Equipamentos.AddAsync(equipamento, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
