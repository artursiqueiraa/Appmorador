using AppMorador.Api.Auth;
using AppMorador.Application.Veiculos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="CredenciaisController"/>: Create/List aninhados sob o Morador, Update/Delete pelo Id do Veículo.</summary>
[ApiController]
[Authorize]
public sealed class VeiculosController : ControllerBase
{
    private readonly IVeiculoServico _veiculoServico;

    public VeiculosController(IVeiculoServico veiculoServico)
    {
        _veiculoServico = veiculoServico;
    }

    [HttpPost("api/moradores/{moradorId:guid}/veiculos")]
    public async Task<IActionResult> Create(Guid moradorId, [FromBody] CriarVeiculoRequest request, CancellationToken cancellationToken)
    {
        var result = await _veiculoServico.CreateAsync(User.GetUsuarioId(), moradorId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/moradores/{moradorId:guid}/veiculos")]
    public async Task<IActionResult> List(Guid moradorId, CancellationToken cancellationToken)
    {
        var result = await _veiculoServico.ListByMoradorAsync(User.GetUsuarioId(), moradorId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/veiculos/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarVeiculoRequest request, CancellationToken cancellationToken)
    {
        var result = await _veiculoServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/veiculos/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _veiculoServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
