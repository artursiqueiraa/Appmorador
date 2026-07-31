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

    public async Task<IReadOnlyDictionary<StatusEquipamento, int>> ContarPorStatusGlobalAsync(CancellationToken cancellationToken)
    {
        var grupos = await _db.Equipamentos
            .AsNoTracking()
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return grupos.ToDictionary(g => g.Status, g => g.Quantidade);
    }

    public Task<DateTime?> GetUltimaSincronizacaoAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _db.Equipamentos
            .Where(e => e.PropriedadeId == propriedadeId && e.UltimaSincronizacaoUtc != null)
            .OrderByDescending(e => e.UltimaSincronizacaoUtc)
            .Select(e => (DateTime?)e.UltimaSincronizacaoUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<Equipamento> Itens, int Total)> ListarGlobalAsync(
        int pagina, int tamanhoPagina, string? busca, FabricanteEquipamento? fabricante,
        EstadoOperacionalEquipamento? estadoOperacional, bool incluirRemovidos, CancellationToken cancellationToken)
    {
        var query = incluirRemovidos
            ? _db.Equipamentos.IgnoreQueryFilters().Include(e => e.Propriedade).Include(e => e.ModeloEquipamento).AsQueryable()
            : _db.Equipamentos.Include(e => e.Propriedade).Include(e => e.ModeloEquipamento).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = busca.Trim();
            query = query.Where(e => EF.Functions.Like(e.Nome, $"%{termo}%") || (e.Identificador != null && EF.Functions.Like(e.Identificador, $"%{termo}%")));
        }

        if (fabricante is not null)
        {
            query = query.Where(e => e.Fabricante == fabricante);
        }

        if (estadoOperacional is not null)
        {
            query = query.Where(e => e.EstadoOperacional == estadoOperacional);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var itens = await query
            .OrderBy(e => e.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    public async Task<bool> ExisteNumeroSerieDuplicadoAsync(
        Guid propriedadeId, string numeroSerie, Guid? excluirEquipamentoId, CancellationToken cancellationToken)
    {
        var query = _db.Equipamentos.Where(e => e.PropriedadeId == propriedadeId && e.Identificador == numeroSerie);
        if (excluirEquipamentoId is not null)
        {
            query = query.Where(e => e.Id != excluirEquipamentoId);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<Equipamento?> GetByFabricanteEIdentificadorAsync(
        FabricanteEquipamento fabricante, string identificador, CancellationToken cancellationToken) =>
        _db.Equipamentos
            .FirstOrDefaultAsync(e => e.Fabricante == fabricante && e.Identificador == identificador, cancellationToken);

    public async Task AddAsync(Equipamento equipamento, CancellationToken cancellationToken) =>
        await _db.Equipamentos.AddAsync(equipamento, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
