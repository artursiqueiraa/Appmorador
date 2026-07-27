using AppMorador.Api.Auth;
using AppMorador.Application.Equipamentos;
using AppMorador.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Sprint 21 (ADR 0027) — catálogo de modelos de equipamento + capacidades. Exclusivo de Técnico/Master (quem instala/configura hardware).</summary>
[ApiController]
[Route("api/modelos-equipamento")]
[Authorize(Policy = Policies.RequerTecnico)]
public sealed class ModelosEquipamentoController : ControllerBase
{
    private readonly IModeloEquipamentoServico _modeloEquipamentoServico;

    public ModelosEquipamentoController(IModeloEquipamentoServico modeloEquipamentoServico)
    {
        _modeloEquipamentoServico = modeloEquipamentoServico;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriarModeloEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var response = await _modeloEquipamentoServico.CriarAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] FabricanteEquipamento? fabricante, CancellationToken cancellationToken)
    {
        var response = await _modeloEquipamentoServico.ListarAsync(fabricante, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:guid}/capacidades")]
    public async Task<IActionResult> DefinirCapacidades(Guid id, [FromBody] DefinirCapacidadesRequest request, CancellationToken cancellationToken)
    {
        var result = await _modeloEquipamentoServico.DefinirCapacidadesAsync(id, request.Capacidades, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
