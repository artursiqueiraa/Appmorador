using AppMorador.Api.Auth;
using AppMorador.Application.Painel.VinculosEquipamento;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 22B (ADR 0031) — alocação Equipamento↔Propriedade para o Painel Web, via
/// <see cref="VinculoEquipamentoPropriedade"/> (entidade nova, deliberadamente separada do
/// <c>Provisionamento</c> já existente desde ADR 0028/Sprint 21 — mesma palavra de negócio,
/// conceitos diferentes). Rota `api/painel/provisionamentos` isolada de qualquer contrato
/// mobile/existente. Master/Técnico-only.
/// </summary>
[ApiController]
[Route("api/painel/provisionamentos")]
[Authorize(Policy = Policies.RequerTecnico)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ProvisionamentosAdminController : ControllerBase
{
    private readonly IVinculoEquipamentoServico _vinculoEquipamentoServico;

    public ProvisionamentosAdminController(IVinculoEquipamentoServico vinculoEquipamentoServico)
    {
        _vinculoEquipamentoServico = vinculoEquipamentoServico;
    }

    /// <summary>Lista os vínculos Equipamento↔Propriedade atualmente ativos (não encerrados), paginado.</summary>
    /// <response code="200">Página de vínculos ativos retornada.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAtivos(
        [FromQuery] int pagina, [FromQuery] int tamanhoPagina, CancellationToken cancellationToken)
    {
        var resultado = await _vinculoEquipamentoServico.ListarAtivosAsync(pagina, tamanhoPagina, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>Totais de alocação: equipamentos, provisionados e disponíveis.</summary>
    /// <response code="200">Totais retornados.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDashboard(CancellationToken cancellationToken)
    {
        var resultado = await _vinculoEquipamentoServico.ObterDashboardAsync(cancellationToken);
        return Ok(resultado);
    }

    /// <summary>Histórico completo de vínculos (ativos e encerrados) de um equipamento — nunca apagado.</summary>
    /// <response code="200">Histórico retornado (pode ser uma lista vazia).</response>
    /// <response code="404">Equipamento não encontrado.</response>
    [HttpGet("equipamentos/{equipamentoId:guid}/historico")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarHistorico(Guid equipamentoId, CancellationToken cancellationToken)
    {
        var resultado = await _vinculoEquipamentoServico.ListarHistoricoAsync(equipamentoId, cancellationToken);
        if (!resultado.Success)
        {
            return NotFound(new { error = resultado.Error });
        }

        return Ok(resultado.Data);
    }

    /// <summary>Provisiona (aloca) um equipamento numa propriedade. Rejeitado se o equipamento já tiver um vínculo ativo em outro lugar.</summary>
    /// <response code="201">Vínculo criado.</response>
    /// <response code="409">Equipamento/Propriedade inexistente, ou equipamento já provisionado em outro lugar.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Provisionar([FromBody] ProvisionarEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _vinculoEquipamentoServico.ProvisionarAsync(
            User.GetUsuarioId(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        if (!resultado.Success)
        {
            return Conflict(new { error = resultado.Error });
        }

        return StatusCode(StatusCodes.Status201Created, resultado.Data);
    }

    /// <summary>Troca o equipamento de uma propriedade: encerra o vínculo antigo e cria um novo (nunca edita em lugar) — histórico preservado.</summary>
    /// <response code="200">Troca concluída, novo vínculo retornado.</response>
    /// <response code="409">Equipamento antigo não provisionado nesta propriedade, ou equipamento novo já provisionado em outro lugar.</response>
    [HttpPost("trocar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Trocar([FromBody] TrocarEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _vinculoEquipamentoServico.TrocarAsync(
            User.GetUsuarioId(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        if (!resultado.Success)
        {
            return Conflict(new { error = resultado.Error });
        }

        return Ok(resultado.Data);
    }

    /// <summary>Desvincula (libera) um equipamento — encerra o vínculo ativo, preservando histórico.</summary>
    /// <response code="204">Equipamento desvinculado.</response>
    /// <response code="404">Este equipamento não está provisionado em nenhuma propriedade.</response>
    [HttpDelete("equipamentos/{equipamentoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desvincular(Guid equipamentoId, CancellationToken cancellationToken)
    {
        var resultado = await _vinculoEquipamentoServico.DesvincularAsync(
            User.GetUsuarioId(), equipamentoId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        if (!resultado.Success)
        {
            return NotFound(new { error = resultado.Error });
        }

        return NoContent();
    }
}
