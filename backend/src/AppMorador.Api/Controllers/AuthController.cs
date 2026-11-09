using AppMorador.Api.Auth;
using AppMorador.Application.Autenticacao;
using AppMorador.Application.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppMorador.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAutenticacaoServico _autenticacaoServico;
    private readonly IImpersonationServico _impersonationServico;

    public AuthController(IAutenticacaoServico autenticacaoServico, IImpersonationServico impersonationServico)
    {
        _autenticacaoServico = autenticacaoServico;
        _impersonationServico = impersonationServico;
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimiterPolicies.Auth)]
    public async Task<IActionResult> Register([FromBody] CadastrarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var result = await _autenticacaoServico.RegisterAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Conflict(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, new { id = result.Data });
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimiterPolicies.Auth)]
    public async Task<IActionResult> Login([FromBody] EntrarRequest request, CancellationToken cancellationToken)
    {
        var result = await _autenticacaoServico.LoginAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(ResponderSessao(result.Data!));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        var token = TokenDaRequisicao(request?.RefreshToken);
        if (token is null)
        {
            return Unauthorized(new { error = "Sessão expirada. Faça login novamente." });
        }

        var result = await _autenticacaoServico.RefreshAsync(new RefreshRequest { RefreshToken = token }, cancellationToken);
        if (!result.Success)
        {
            ApagarCookieSessao();
            return Unauthorized(new { error = result.Error });
        }

        return Ok(ResponderSessao(result.Data!));
    }

    /// <summary>
    /// Sem [Authorize] desde a Sprint 27: no web, depois de recarregar a página o access token (só em
    /// memória) já não existe — quem prova a sessão é o refresh token (cookie ou corpo), que é revogado.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] SairRequest? request, CancellationToken cancellationToken)
    {
        var token = TokenDaRequisicao(request?.RefreshToken);
        if (token is not null)
        {
            await _autenticacaoServico.LogoutAsync(token, cancellationToken);
        }

        ApagarCookieSessao();
        return NoContent();
    }

    // --- Sprint 27 (ADR 0043, dívida 51) — sessão web em cookie HttpOnly ---------------------
    // O app web se identifica com "X-Cliente: web": o refresh token vai num cookie HttpOnly +
    // SameSite=Strict, restrito a /api/auth, e some do corpo da resposta (o JavaScript da página
    // nunca o vê). O celular continua recebendo o token no corpo e guardando no armazenamento
    // seguro do aparelho.
    public const string CookieSessao = "akteck_sessao";

    private bool ClienteWeb => string.Equals(Request.Headers["X-Cliente"].FirstOrDefault(), "web", StringComparison.OrdinalIgnoreCase);

    private string? TokenDaRequisicao(string? doCorpo) =>
        !string.IsNullOrWhiteSpace(doCorpo) ? doCorpo
        : Request.Cookies.TryGetValue(CookieSessao, out var doCookie) && !string.IsNullOrWhiteSpace(doCookie) ? doCookie
        : null;

    private EntrarResponse ResponderSessao(EntrarResponse sessao)
    {
        if (!ClienteWeb)
        {
            return sessao;
        }

        Response.Cookies.Append(CookieSessao, sessao.RefreshToken, OpcoesCookie(DateTimeOffset.UtcNow.AddDays(30)));
        return new EntrarResponse
        {
            AccessToken = sessao.AccessToken,
            RefreshToken = string.Empty,
            ExpiresInSeconds = sessao.ExpiresInSeconds,
            UsuarioId = sessao.UsuarioId,
            Nome = sessao.Nome,
            Email = sessao.Email,
        };
    }

    private void ApagarCookieSessao()
    {
        if (Request.Cookies.ContainsKey(CookieSessao))
        {
            Response.Cookies.Delete(CookieSessao, OpcoesCookie(null));
        }
    }

    private CookieOptions OpcoesCookie(DateTimeOffset? expira) => new()
    {
        HttpOnly = true,
        // Navegadores aceitam cookie Secure em http://localhost; fora dele, só HTTPS.
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = expira,
        IsEssential = true,
    };

    /// <summary>Sprint 21 (ADR 0021, Fase 3) — Master/Suporte "entram como" um cliente. Técnico não tem esta capacidade (ver Policies.RequerSuporte).</summary>
    [HttpPost("impersonar")]
    [Authorize(Policy = Policies.RequerSuporte)]
    public async Task<IActionResult> Impersonar([FromBody] ImpersonarRequest request, CancellationToken cancellationToken)
    {
        var result = await _impersonationServico.IniciarAsync(
            User.GetUsuarioId(), request.PropriedadeId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("impersonar/encerrar")]
    [Authorize(Policy = Policies.RequerSuporte)]
    public async Task<IActionResult> EncerrarImpersonation([FromBody] ImpersonarRequest request, CancellationToken cancellationToken)
    {
        await _impersonationServico.EncerrarAsync(
            User.GetUsuarioId(), request.PropriedadeId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return NoContent();
    }
}
