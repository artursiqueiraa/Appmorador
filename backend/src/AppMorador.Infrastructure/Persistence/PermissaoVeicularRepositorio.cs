using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class PermissaoVeicularRepositorio : IPermissaoVeicularRepositorio
{
    private readonly AppDbContext _db;

    public PermissaoVeicularRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<PermissaoVeicular?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.PermissoesVeiculares
            .Include(p => p.Veiculo)
            .ThenInclude(v => v!.Morador)
            .ThenInclude(m => m!.Unidade)
            .ThenInclude(u => u!.Propriedade)
            .Include(p => p.PontoAcesso)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    // Sem AsNoTracking em nenhum metodo abaixo: usados tanto para exibicao quanto
    // pelos varios niveis de cascade de exclusao (Veiculo/Unidade/Morador/Propriedade/PontoAcesso).
    public async Task<IReadOnlyList<PermissaoVeicular>> ListByVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken) =>
        await _db.PermissoesVeiculares
            .Include(p => p.PontoAcesso)
            .Where(p => p.VeiculoId == veiculoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PermissaoVeicular>> ListByVeiculosAsync(IReadOnlyList<Guid> veiculoIds, CancellationToken cancellationToken) =>
        await _db.PermissoesVeiculares
            .Where(p => veiculoIds.Contains(p.VeiculoId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PermissaoVeicular>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.PermissoesVeiculares
            .Where(p => p.Veiculo!.Morador!.Unidade!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PermissaoVeicular>> ListByPontoAcessoAsync(Guid pontoAcessoId, CancellationToken cancellationToken) =>
        await _db.PermissoesVeiculares
            .Where(p => p.PontoAcessoId == pontoAcessoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(PermissaoVeicular permissaoVeicular, CancellationToken cancellationToken) =>
        await _db.PermissoesVeiculares.AddAsync(permissaoVeicular, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
