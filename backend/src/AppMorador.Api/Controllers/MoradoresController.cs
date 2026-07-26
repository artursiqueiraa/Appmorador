using AppMorador.Api.Auth;
using AppMorador.Application.Moradores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="UnidadesController"/>: Create/List aninhados sob a Unidade, Update/Delete pelo Id do Morador.</summary>
[ApiController]
[Authorize]
public sealed class MoradoresController : ControllerBase
{
    private readonly IMoradorServico _moradorServico;

    public MoradoresController(IMoradorServico moradorServico)
    {
        _moradorServico = moradorServico;
    }

    [HttpPost("api/unidades/{unidadeId:guid}/moradores")]
    public async Task<IActionResult> Create(Guid unidadeId, [FromBody] CriarMoradorRequest request, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.CreateAsync(User.GetUsuarioId(), unidadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/unidades/{unidadeId:guid}/moradores")]
    public async Task<IActionResult> List(Guid unidadeId, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.ListByUnidadeAsync(User.GetUsuarioId(), unidadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/moradores/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarMoradorRequest request, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/moradores/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
