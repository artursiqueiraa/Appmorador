using AppMorador.Api.Auth;
using AppMorador.Application.Unidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Create/List são aninhados sob a Propriedade (precisam saber em qual propriedade
/// criar/listar); Update/Delete usam só o Id da Unidade — o ownership é resolvido
/// subindo até a Propriedade dentro do serviço, mesmo padrão de
/// <see cref="PropertiesController"/>.
/// </summary>
[ApiController]
[Authorize]
public sealed class UnidadesController : ControllerBase
{
    private readonly IUnidadeServico _unidadeServico;

    public UnidadesController(IUnidadeServico unidadeServico)
    {
        _unidadeServico = unidadeServico;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/unidades")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarUnidadeRequest request, CancellationToken cancellationToken)
    {
        var result = await _unidadeServico.CreateAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/unidades")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _unidadeServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/unidades/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarUnidadeRequest request, CancellationToken cancellationToken)
    {
        var result = await _unidadeServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/unidades/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _unidadeServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
