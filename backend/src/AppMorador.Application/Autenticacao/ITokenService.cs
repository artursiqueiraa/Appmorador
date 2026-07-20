using AppMorador.Domain.Entities;

namespace AppMorador.Application.Autenticacao;

/// <summary>Porta de geracao de tokens — implementacao (JWT + SHA-256) fica em Infrastructure.</summary>
public interface ITokenService
{
    string GenerateAccessToken(Usuario usuario);

    /// <summary>Bytes aleatorios em base64 — o valor cru devolvido ao cliente. Nunca persistido em texto puro.</summary>
    string GenerateRefreshToken();

    /// <summary>Hash (SHA-256) usado para procurar/comparar o refresh token no banco.</summary>
    string HashToken(string rawToken);

    TimeSpan AccessTokenLifetime { get; }

    TimeSpan RefreshTokenLifetime { get; }
}
