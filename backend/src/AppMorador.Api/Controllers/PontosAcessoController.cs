using AppMorador.Api.Auth;
using AppMorador.Application.PontosAcesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="UnidadesController"/>: Create/List aninhados sob a Propriedade, Update/Delete pelo Id do Ponto de Acesso.</summary>
[ApiController]
[Authorize]
public sealed class PontosAcessoController : ControllerBase
{
    private readonly IPontoAcessoServico _pontoAcessoServico;

    public PontosAcessoController(IPontoAcessoServico pontoAcessoServico)
    {
        _pontoAcessoServico = pontoAcessoServico;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/pontos-acesso")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.CreateAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/pontos-acesso")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/pontos-acesso/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/pontos-acesso/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _pontoAcessoServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
