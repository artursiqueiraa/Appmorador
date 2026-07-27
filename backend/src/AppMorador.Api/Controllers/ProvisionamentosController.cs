using AppMorador.Api.Auth;
using AppMorador.Application.Provisionamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 21 (ADR 0028) — "pacote de instalação" de uma Propriedade. Exclusivo de
/// Técnico/Master (quem implanta clientes) — sem ownership check por
/// ProprietarioId, o cliente não gerencia isto.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequerTecnico)]
public sealed class ProvisionamentosController : ControllerBase
{
    private readonly IProvisionamentoServico _provisionamentoServico;

    public ProvisionamentosController(IProvisionamentoServico provisionamentoServico)
    {
        _provisionamentoServico = provisionamentoServico;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/provisionamentos")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarProvisionamentoRequest request, CancellationToken cancellationToken)
    {
        var result = await _provisionamentoServico.CriarAsync(propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/provisionamentos")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _provisionamentoServico.ListarAsync(propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/provisionamentos/{id:guid}/arquivar")]
    public async Task<IActionResult> Arquivar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _provisionamentoServico.ArquivarAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
