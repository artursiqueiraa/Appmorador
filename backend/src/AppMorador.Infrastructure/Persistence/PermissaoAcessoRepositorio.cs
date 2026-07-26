using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class PermissaoAcessoRepositorio : IPermissaoAcessoRepositorio
{
    private readonly AppDbContext _db;

    public PermissaoAcessoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<PermissaoAcesso?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.PermissoesAcesso
            .Include(p => p.Credencial)
            .ThenInclude(c => c!.Morador)
            .ThenInclude(m => m!.Unidade)
            .ThenInclude(u => u!.Propriedade)
            .Include(p => p.PontoAcesso)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    // Sem AsNoTracking em nenhum metodo abaixo: usados tanto para exibicao quanto
    // para os varios niveis de cascade de exclusao (Credencial/Unidade/Propriedade/
    // PontoAcesso), todos precisam rastrear e salvar as mudancas.
    public async Task<IReadOnlyList<PermissaoAcesso>> ListByCredencialAsync(Guid credencialId, CancellationToken cancellationToken) =>
        await _db.PermissoesAcesso
            .Include(p => p.PontoAcesso)
            .Where(p => p.CredencialId == credencialId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PermissaoAcesso>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken) =>
        await _db.PermissoesAcesso
            .Where(p => p.Credencial!.MoradorId == moradorId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PermissaoAcesso>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken) =>
        await _db.PermissoesAcesso
            .Where(p => p.Credencial!.Morador!.UnidadeId == unidadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PermissaoAcesso>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        await _db.PermissoesAcesso
            .Where(p => p.Credencial!.Morador!.Unidade!.PropriedadeId == propriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<PermissaoAcesso>> ListByPontoAcessoAsync(Guid pontoAcessoId, CancellationToken cancellationToken) =>
        await _db.PermissoesAcesso
            .Where(p => p.PontoAcessoId == pontoAcessoId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(PermissaoAcesso permissaoAcesso, CancellationToken cancellationToken) =>
        await _db.PermissoesAcesso.AddAsync(permissaoAcesso, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
