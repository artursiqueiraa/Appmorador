using AppMorador.Api.Auth;
using AppMorador.Application.PermissoesAcesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="MoradoresController"/>: Create/List aninhados sob a Credencial, Update/Delete pelo Id da Permissão.</summary>
[ApiController]
[Authorize]
public sealed class PermissoesAcessoController : ControllerBase
{
    private readonly IPermissaoAcessoServico _permissaoAcessoServico;

    public PermissoesAcessoController(IPermissaoAcessoServico permissaoAcessoServico)
    {
        _permissaoAcessoServico = permissaoAcessoServico;
    }

    [HttpPost("api/credenciais/{credencialId:guid}/permissoes")]
    public async Task<IActionResult> Create(Guid credencialId, [FromBody] CriarPermissaoAcessoRequest request, CancellationToken cancellationToken)
    {
        var result = await _permissaoAcessoServico.CreateAsync(User.GetUsuarioId(), credencialId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/credenciais/{credencialId:guid}/permissoes")]
    public async Task<IActionResult> List(Guid credencialId, CancellationToken cancellationToken)
    {
        var result = await _permissaoAcessoServico.ListByCredencialAsync(User.GetUsuarioId(), credencialId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/permissoes/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarPermissaoAcessoRequest request, CancellationToken cancellationToken)
    {
        var result = await _permissaoAcessoServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/permissoes/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _permissaoAcessoServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
