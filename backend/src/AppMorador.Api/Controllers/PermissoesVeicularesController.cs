using AppMorador.Api.Auth;
using AppMorador.Application.PermissoesVeiculares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="PermissoesAcessoController"/>: Create/List aninhados sob o Veículo, Delete pelo Id da Permissão.</summary>
[ApiController]
[Authorize]
public sealed class PermissoesVeicularesController : ControllerBase
{
    private readonly IPermissaoVeicularServico _permissaoVeicularServico;

    public PermissoesVeicularesController(IPermissaoVeicularServico permissaoVeicularServico)
    {
        _permissaoVeicularServico = permissaoVeicularServico;
    }

    [HttpPost("api/veiculos/{veiculoId:guid}/permissoes-veiculares")]
    public async Task<IActionResult> Create(Guid veiculoId, [FromBody] CriarPermissaoVeicularRequest request, CancellationToken cancellationToken)
    {
        var result = await _permissaoVeicularServico.CreateAsync(User.GetUsuarioId(), veiculoId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/veiculos/{veiculoId:guid}/permissoes-veiculares")]
    public async Task<IActionResult> List(Guid veiculoId, CancellationToken cancellationToken)
    {
        var result = await _permissaoVeicularServico.ListByVeiculoAsync(User.GetUsuarioId(), veiculoId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/permissoes-veiculares/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _permissaoVeicularServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
