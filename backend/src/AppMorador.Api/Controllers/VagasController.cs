using AppMorador.Api.Auth;
using AppMorador.Application.Vagas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="PontosAcessoController"/>: Create/List aninhados sob a Propriedade, Update/Status/Delete pelo Id da Vaga.</summary>
[ApiController]
[Authorize]
public sealed class VagasController : ControllerBase
{
    private readonly IVagaServico _vagaServico;

    public VagasController(IVagaServico vagaServico)
    {
        _vagaServico = vagaServico;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/vagas")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarVagaRequest request, CancellationToken cancellationToken)
    {
        var result = await _vagaServico.CreateAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/vagas")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _vagaServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/vagas/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarVagaRequest request, CancellationToken cancellationToken)
    {
        var result = await _vagaServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/vagas/{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarStatusVagaRequest request, CancellationToken cancellationToken)
    {
        var result = await _vagaServico.AtualizarStatusAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/vagas/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vagaServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
