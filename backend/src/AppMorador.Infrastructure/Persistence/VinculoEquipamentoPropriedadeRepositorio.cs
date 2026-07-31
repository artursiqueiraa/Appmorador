using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class VinculoEquipamentoPropriedadeRepositorio : IVinculoEquipamentoPropriedadeRepositorio
{
    private readonly AppDbContext _db;

    public VinculoEquipamentoPropriedadeRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<VinculoEquipamentoPropriedade?> GetVinculoAtivoPorEquipamentoAsync(Guid equipamentoId, CancellationToken cancellationToken) =>
        _db.VinculosEquipamentoPropriedade
            .Include(v => v.Propriedade)
            .FirstOrDefaultAsync(v => v.EquipamentoId == equipamentoId && v.DataFimUtc == null, cancellationToken);

    public async Task<IReadOnlyList<VinculoEquipamentoPropriedade>> ListarHistoricoPorEquipamentoAsync(
        Guid equipamentoId, CancellationToken cancellationToken) =>
        await _db.VinculosEquipamentoPropriedade
            .AsNoTracking()
            .Include(v => v.Propriedade)
            .Include(v => v.Equipamento)
            .Where(v => v.EquipamentoId == equipamentoId)
            .OrderByDescending(v => v.DataInicioUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<VinculoEquipamentoPropriedade> Itens, int Total)> ListarAtivosGlobalAsync(
        int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        var query = _db.VinculosEquipamentoPropriedade
            .AsNoTracking()
            .Include(v => v.Propriedade)
            .Include(v => v.Equipamento)
            .Where(v => v.DataFimUtc == null);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var itens = await query
            .OrderByDescending(v => v.DataInicioUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }

    public Task<int> ContarEquipamentosProvisionadosAsync(CancellationToken cancellationToken) =>
        _db.VinculosEquipamentoPropriedade.CountAsync(v => v.DataFimUtc == null, cancellationToken);

    public async Task AddAsync(VinculoEquipamentoPropriedade vinculo, CancellationToken cancellationToken) =>
        await _db.VinculosEquipamentoPropriedade.AddAsync(vinculo, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
