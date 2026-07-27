using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class UsuarioPropriedadePermissaoRepositorio : IUsuarioPropriedadePermissaoRepositorio
{
    private readonly AppDbContext _db;

    public UsuarioPropriedadePermissaoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PermissaoFuncionalidade>> ListAsync(Guid usuarioPropriedadeId, CancellationToken cancellationToken) =>
        await _db.UsuariosPropriedadePermissao
            .Where(p => p.UsuarioPropriedadeId == usuarioPropriedadeId)
            .Select(p => p.Permissao)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<bool> TemPermissaoAsync(Guid usuarioPropriedadeId, PermissaoFuncionalidade permissao, CancellationToken cancellationToken) =>
        _db.UsuariosPropriedadePermissao
            .AnyAsync(p => p.UsuarioPropriedadeId == usuarioPropriedadeId && p.Permissao == permissao, cancellationToken);

    public async Task SubstituirAsync(Guid usuarioPropriedadeId, IReadOnlyCollection<PermissaoFuncionalidade> permissoes, CancellationToken cancellationToken)
    {
        var existentes = await _db.UsuariosPropriedadePermissao
            .Where(p => p.UsuarioPropriedadeId == usuarioPropriedadeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _db.UsuariosPropriedadePermissao.RemoveRange(existentes);
        _db.UsuariosPropriedadePermissao.AddRange(permissoes.Distinct().Select(p => new UsuarioPropriedadePermissao
        {
            Id = Guid.NewGuid(),
            UsuarioPropriedadeId = usuarioPropriedadeId,
            Permissao = p,
        }));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
