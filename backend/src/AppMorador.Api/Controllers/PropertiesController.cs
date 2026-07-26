using AppMorador.Api.Auth;
using AppMorador.Application.Propriedades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

[ApiController]
[Route("api/properties")]
[Authorize]
public sealed class PropertiesController : ControllerBase
{
    private readonly IPropriedadeServico _propriedadeServico;

    public PropertiesController(IPropriedadeServico propriedadeServico)
    {
        _propriedadeServico = propriedadeServico;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriarPropriedadeRequest request, CancellationToken cancellationToken)
    {
        var dto = await _propriedadeServico.CreateAsync(User.GetUsuarioId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var list = await _propriedadeServico.ListByOwnerAsync(User.GetUsuarioId(), cancellationToken);
        return Ok(list);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarPropriedadeRequest request, CancellationToken cancellationToken)
    {
        var result = await _propriedadeServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _propriedadeServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }
}
