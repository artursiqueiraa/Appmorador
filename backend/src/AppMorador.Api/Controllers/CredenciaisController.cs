using AppMorador.Api.Auth;
using AppMorador.Application.Credenciais;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Create/List são aninhados sob o Morador; Status/Delete usam só o Id da Credencial —
/// mesmo padrão de <see cref="MoradoresController"/>. Tipo é imutável após a criação
/// (ver <see cref="AtualizarStatusCredencialRequest"/>), por isso não há um Update genérico.
/// </summary>
[ApiController]
[Authorize]
public sealed class CredenciaisController : ControllerBase
{
    private readonly ICredencialServico _credencialServico;

    public CredenciaisController(ICredencialServico credencialServico)
    {
        _credencialServico = credencialServico;
    }

    [HttpPost("api/moradores/{moradorId:guid}/credenciais")]
    public async Task<IActionResult> Create(Guid moradorId, [FromBody] CriarCredencialRequest request, CancellationToken cancellationToken)
    {
        var result = await _credencialServico.CreateAsync(User.GetUsuarioId(), moradorId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/moradores/{moradorId:guid}/credenciais")]
    public async Task<IActionResult> List(Guid moradorId, CancellationToken cancellationToken)
    {
        var result = await _credencialServico.ListByMoradorAsync(User.GetUsuarioId(), moradorId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/credenciais/{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarStatusCredencialRequest request, CancellationToken cancellationToken)
    {
        var result = await _credencialServico.AtualizarStatusAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/credenciais/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _credencialServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
