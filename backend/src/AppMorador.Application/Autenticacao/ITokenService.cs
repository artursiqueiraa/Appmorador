using AppMorador.Domain.Entities;

namespace AppMorador.Application.Autenticacao;

/// <summary>Porta de geracao de tokens — implementacao (JWT + SHA-256) fica em Infrastructure.</summary>
public interface ITokenService
{
    string GenerateAccessToken(Usuario usuario);

    /// <summary>
    /// Sprint 21 (ADR 0021) — token de impersonation: claims do usuário ALVO (quem
    /// está sendo visualizado), mais <c>impersonating=true</c> e
    /// <c>impersonatedBy</c> (Id do Master/Suporte que iniciou). Vida curta fixa
    /// (<see cref="ImpersonationTokenLifetime"/>), nunca acompanha
    /// <see cref="AccessTokenLifetime"/> — e nunca gera refresh token (ver
    /// <see cref="AppMorador.Application.Autenticacao.IAutenticacaoServico"/>, que
    /// não persiste nenhum <c>RefreshToken</c> para este fluxo).
    /// </summary>
    string GenerateImpersonationToken(Usuario usuarioAlvo, Guid masterId, string masterNome);

    /// <summary>Bytes aleatorios em base64 — o valor cru devolvido ao cliente. Nunca persistido em texto puro.</summary>
    string GenerateRefreshToken();

    /// <summary>Hash (SHA-256) usado para procurar/comparar o refresh token no banco.</summary>
    string HashToken(string rawToken);

    TimeSpan AccessTokenLifetime { get; }

    TimeSpan RefreshTokenLifetime { get; }

    TimeSpan ImpersonationTokenLifetime { get; }
}
