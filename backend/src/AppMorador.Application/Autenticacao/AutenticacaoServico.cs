using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Autenticacao;

/// <summary>
/// Regras de negocio de autenticacao: registro, login (com lockout), refresh
/// (com rotacao) e logout (revogacao). Nunca revela, na mensagem de erro, se foi o
/// e-mail ou a senha que estava errado (evita user enumeration).
/// </summary>
public sealed class AutenticacaoServico : IAutenticacaoServico
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const string InvalidCredentialsMessage = "E-mail ou senha inválidos.";

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AutenticacaoServico(IUsuarioRepositorio usuarios, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<Result<Guid>> RegisterAsync(CadastrarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var existing = await _usuarios.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result<Guid>.Fail("Não foi possível concluir o cadastro.");
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Email = email,
            SenhaHash = _passwordHasher.Hash(request.Senha),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _usuarios.AddAsync(usuario, cancellationToken).ConfigureAwait(false);
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<Guid>.Ok(usuario.Id);
    }

    public async Task<Result<EntrarResponse>> LoginAsync(EntrarRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var usuario = await _usuarios.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (usuario is null)
        {
            return Result<EntrarResponse>.Fail(InvalidCredentialsMessage);
        }

        if (usuario.BloqueadoAteUtc is { } bloqueadoAte && bloqueadoAte > DateTime.UtcNow)
        {
            return Result<EntrarResponse>.Fail("Conta temporariamente bloqueada. Tente novamente mais tarde.");
        }

        if (!_passwordHasher.Verify(request.Senha, usuario.SenhaHash))
        {
            usuario.TentativasFalhas++;
            if (usuario.TentativasFalhas >= MaxFailedAttempts)
            {
                usuario.BloqueadoAteUtc = DateTime.UtcNow.Add(LockoutDuration);
                usuario.TentativasFalhas = 0;
            }

            await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<EntrarResponse>.Fail(InvalidCredentialsMessage);
        }

        usuario.TentativasFalhas = 0;
        usuario.BloqueadoAteUtc = null;

        var entrarResponse = await IssueTokensAsync(usuario, cancellationToken).ConfigureAwait(false);
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EntrarResponse>.Ok(entrarResponse);
    }

    public async Task<Result<EntrarResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        const string expiredMessage = "Sessão expirada. Faça login novamente.";

        var hash = _tokenService.HashToken(request.RefreshToken);
        var existingToken = await _usuarios.GetRefreshTokenByHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (existingToken is null || !existingToken.IsActive)
        {
            return Result<EntrarResponse>.Fail(expiredMessage);
        }

        var usuario = await _usuarios.GetByIdAsync(existingToken.UsuarioId, cancellationToken).ConfigureAwait(false);
        if (usuario is null)
        {
            return Result<EntrarResponse>.Fail(expiredMessage);
        }

        var newRawToken = _tokenService.GenerateRefreshToken();
        var newHash = _tokenService.HashToken(newRawToken);

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.ReplacedByTokenHash = newHash;

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = newHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime),
        };

        await _usuarios.AddRefreshTokenAsync(newRefreshToken, cancellationToken).ConfigureAwait(false);
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var accessToken = _tokenService.GenerateAccessToken(usuario);

        return Result<EntrarResponse>.Ok(new EntrarResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRawToken,
            ExpiresInSeconds = (int)_tokenService.AccessTokenLifetime.TotalSeconds,
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
        });
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = _tokenService.HashToken(refreshToken);
        var existingToken = await _usuarios.GetRefreshTokenByHashAsync(hash, cancellationToken).ConfigureAwait(false);

        if (existingToken is not null && existingToken.RevokedAtUtc is null)
        {
            existingToken.RevokedAtUtc = DateTime.UtcNow;
            await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<EntrarResponse> IssueTokensAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(usuario);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = _tokenService.HashToken(rawRefreshToken),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(_tokenService.RefreshTokenLifetime),
        };

        await _usuarios.AddRefreshTokenAsync(refreshToken, cancellationToken).ConfigureAwait(false);

        return new EntrarResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresInSeconds = (int)_tokenService.AccessTokenLifetime.TotalSeconds,
            UsuarioId = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
