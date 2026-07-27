using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AppMorador.Application.Autenticacao;
using AppMorador.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace AppMorador.Infrastructure.Identity;

internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(_options.AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    // Sprint 21 (ADR 0021, Fase 3.3) — fixo em 15 minutos, nao configuravel via
    // appsettings de proposito: a missao especifica esse valor como regra de
    // seguranca, nao como parametro de ambiente.
    public TimeSpan ImpersonationTokenLifetime => TimeSpan.FromMinutes(15);

    public string GenerateAccessToken(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new("securityStamp", usuario.SecurityStamp.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Sprint 21 (ADR 0021) — so presente para os 3 papeis internos; um cliente
        // nunca tem essa claim (RoleGlobal e null), entao Policies que checam a
        // AUSENCIA da claim "role" identificam corretamente um usuario cliente.
        if (usuario.RoleGlobal is not null)
        {
            claims.Add(new Claim("role", usuario.RoleGlobal.Value.ToString()));
        }

        return EmitirToken(claims, AccessTokenLifetime);
    }

    public string GenerateImpersonationToken(Usuario usuarioAlvo, Guid masterId, string masterNome)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioAlvo.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuarioAlvo.Email),
            new("securityStamp", usuarioAlvo.SecurityStamp.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("impersonating", "true"),
            new("impersonatedBy", masterId.ToString()),
            new("impersonatedByNome", masterNome),
        };

        // Sprint 21 — o alvo de impersonation e sempre um cliente (Administrador via
        // ProprietarioId, ver ADR 0021); nunca emite claim "role" aqui, mesmo que o
        // alvo por algum motivo tivesse uma (cenario que nao deveria acontecer, mas
        // a ausencia da claim "role" e o sinal que as Policies de cliente usam).
        return EmitirToken(claims, ImpersonationTokenLifetime);
    }

    private string EmitirToken(IEnumerable<Claim> claims, TimeSpan duracao)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(duracao),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
