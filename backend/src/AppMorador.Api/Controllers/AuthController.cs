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

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _autenticacaoServico.RefreshAsync(request, cancellationToken);
        if (!result.Success)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] SairRequest request, CancellationToken cancellationToken)
    {
        await _autenticacaoServico.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

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
