using AppMorador.Api.Auth;
using AppMorador.Application.Entregas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Create/List aninhados sob a Propriedade (visão unificada de entregas de toda a
/// propriedade, não por morador individual — ver ADR 0013); Update/Status/Delete/Get
/// usam só o Id da Entrega.
/// </summary>
[ApiController]
[Authorize]
public sealed class EntregasController : ControllerBase
{
    private readonly IEntregaServico _entregaServico;

    public EntregasController(IEntregaServico entregaServico)
    {
        _entregaServico = entregaServico;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/entregas")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarEntregaRequest request, CancellationToken cancellationToken)
    {
        var result = await _entregaServico.CreateAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/entregas")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _entregaServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("api/entregas/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _entregaServico.GetByIdAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/entregas/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarEntregaRequest request, CancellationToken cancellationToken)
    {
        var result = await _entregaServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/entregas/{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarStatusEntregaRequest request, CancellationToken cancellationToken)
    {
        var result = await _entregaServico.AtualizarStatusAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/entregas/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _entregaServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
