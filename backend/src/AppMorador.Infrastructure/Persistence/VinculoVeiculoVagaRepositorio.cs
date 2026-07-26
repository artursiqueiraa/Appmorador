using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class VinculoVeiculoVagaRepositorio : IVinculoVeiculoVagaRepositorio
{
    private readonly AppDbContext _db;

    public VinculoVeiculoVagaRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<VinculoVeiculoVaga?> GetAtivoByVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        _db.VinculosVeiculoVaga
            .Include(v => v.Vaga)
            .FirstOrDefaultAsync(v => v.VeiculoId == veiculoId && v.DataFimUtc == null, cancellationToken);

    public Task<VinculoVeiculoVaga?> GetAtivoByVagaAsync(Guid vagaId, CancellationToken cancellationToken) =>
        _db.VinculosVeiculoVaga
            .Include(v => v.Veiculo)
            .FirstOrDefaultAsync(v => v.VagaId == vagaId && v.DataFimUtc == null, cancellationToken);

    // Sem AsNoTracking em nenhum metodo abaixo: usados pelo cascade de exclusao em
    // algum nivel (Veiculo/Vaga/Unidade/Morador/Propriedade), alem de exibicao.
    public async Task<IReadOnlyList<VinculoVeiculoVaga>> ListByVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        await _db.VinculosVeiculoVaga
            .Include(v => v.Vaga)
            .Where(v => v.VeiculoId == veiculoId)
            .OrderByDescending(v => v.DataInicioUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<VinculoVeiculoVaga>> ListByVagaAsync(Guid vagaId, CancellationToken cancellationToken) =>
        await _db.VinculosVeiculoVaga
            .Where(v => v.VagaId == vagaId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<VinculoVeiculoVaga>> ListByVeiculosAsync(IReadOnlyList<Guid> veiculoIds, CancellationToken cancellationToken) =>
        await _db.VinculosVeiculoVaga
            .Where(v => veiculoIds.Contains(v.VeiculoId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<VinculoVeiculoVaga>> ListAtivosByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.VinculosVeiculoVaga
            .Where(v => v.Vaga!.PropriedadeId == propriedadeId && v.DataFimUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<VinculoVeiculoVaga>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.VinculosVeiculoVaga
            .Where(v => v.Vaga!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(VinculoVeiculoVaga vinculo, CancellationToken cancellationToken) =>
        await _db.VinculosVeiculoVaga.AddAsync(vinculo, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
