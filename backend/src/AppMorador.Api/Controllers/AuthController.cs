using AppMorador.Application.Autenticacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppMorador.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAutenticacaoServico _autenticacaoServico;

    public AuthController(IAutenticacaoServico autenticacaoServico)
    {
        _autenticacaoServico = autenticacaoServico;
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
}
