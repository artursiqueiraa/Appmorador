using AppMorador.Api.Auth;
using AppMorador.Application.Painel.Diagnostico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 22B (ADR 0031) — módulo de Diagnóstico do Painel Web. Estritamente somente leitura
/// (só GET nesta Controller) — nunca expõe uma ação que altere estado operacional ou de
/// provisionamento. Master/Técnico-only.
/// </summary>
[ApiController]
[Route("api/diagnostico")]
[Authorize(Policy = Policies.RequerTecnico)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class DiagnosticoController : ControllerBase
{
    private readonly IDiagnosticoServico _diagnosticoServico;

    public DiagnosticoController(IDiagnosticoServico diagnosticoServico)
    {
        _diagnosticoServico = diagnosticoServico;
    }

    /// <summary>
    /// Status agregado de todos os equipamentos (conectividade, estado operacional, último ping,
    /// eventos recentes). Projeção única (Equipamento + StatusCentralJfl + EventoEquipamento) —
    /// nunca altera nenhum dado.
    /// </summary>
    /// <response code="200">Página de status retornada.</response>
    [HttpGet("equipamentos/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterStatusEquipamentos(
        [FromQuery] int pagina, [FromQuery] int tamanhoPagina, CancellationToken cancellationToken)
    {
        var resultado = await _diagnosticoServico.ObterStatusEquipamentosAsync(pagina, tamanhoPagina, cancellationToken);
        return Ok(resultado);
    }
}
