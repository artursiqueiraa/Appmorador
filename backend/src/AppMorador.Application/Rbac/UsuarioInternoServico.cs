using AppMorador.Application.Autenticacao;
using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Rbac;

public sealed class UsuarioInternoServico : IUsuarioInternoServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IPasswordHasher _passwordHasher;

    public UsuarioInternoServico(IUsuarioRepositorio usuarios, IPasswordHasher passwordHasher)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UsuarioInternoResponse>> CriarAsync(CriarUsuarioInternoRequest request, CancellationToken cancellationToken)
    {
        var existente = await _usuarios.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            return Result<UsuarioInternoResponse>.Fail("Já existe uma conta com este e-mail.");
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            SenhaHash = _passwordHasher.Hash(request.Senha),
            RoleGlobal = request.RoleGlobal,
            Ativo = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _usuarios.AddAsync(usuario, cancellationToken).ConfigureAwait(false);
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UsuarioInternoResponse>.Ok(UsuarioInternoResponse.FromEntity(usuario));
    }

    public async Task<IReadOnlyList<UsuarioInternoResponse>> ListarAsync(CancellationToken cancellationToken)
    {
        var internos = await _usuarios.ListInternosAsync(cancellationToken).ConfigureAwait(false);
        return internos.Select(UsuarioInternoResponse.FromEntity).ToList();
    }

    public async Task<Result> DesativarAsync(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (usuario?.RoleGlobal is null)
        {
            return Result.Fail("Conta interna não encontrada.");
        }

        usuario.Ativo = false;
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
