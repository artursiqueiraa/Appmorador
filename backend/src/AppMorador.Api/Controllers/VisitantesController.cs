using AppMorador.Api.Auth;
using AppMorador.Application.Visitantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="PontosAcessoController"/>: Create/List aninhados sob a Propriedade, Update/Delete pelo Id do Visitante.</summary>
[ApiController]
[Authorize]
public sealed class VisitantesController : ControllerBase
{
    private readonly IVisitanteServico _visitanteServico;

    public VisitantesController(IVisitanteServico visitanteServico)
    {
        _visitanteServico = visitanteServico;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/visitantes")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarVisitanteRequest request, CancellationToken cancellationToken)
    {
        var result = await _visitanteServico.CreateAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/visitantes")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _visitanteServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/visitantes/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarVisitanteRequest request, CancellationToken cancellationToken)
    {
        var result = await _visitanteServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/visitantes/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _visitanteServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
