using AppMorador.Api.Auth;
using AppMorador.Application.Painel.Equipamentos;
using AppMorador.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 22B (ADR 0031) — CRUD global (cross-propriedade) de equipamentos para o Painel Web.
/// Prefixo `api/painel/equipamentos` deliberadamente DIFERENTE de `api/equipamentos` (Mobile,
/// `EquipamentosController`) — mesma entidade, contratos e rotas totalmente separados, para
/// nunca colidir com `PUT`/`DELETE /api/equipamentos/{id}` já usados pelo app mobile.
/// Master/Técnico-only (quem instala/configura hardware, ver Sprint 21).
/// </summary>
[ApiController]
[Route("api/painel/equipamentos")]
[Authorize(Policy = Policies.RequerTecnico)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class EquipamentosAdminController : ControllerBase
{
    private readonly IEquipamentoAdminServico _equipamentoAdminServico;

    public EquipamentosAdminController(IEquipamentoAdminServico equipamentoAdminServico)
    {
        _equipamentoAdminServico = equipamentoAdminServico;
    }

    /// <summary>Lista equipamentos, cross-propriedade, paginado. <paramref name="incluirRemovidos"/> exibe também equipamentos excluídos (soft delete) — uso administrativo.</summary>
    /// <response code="200">Página de equipamentos retornada com sucesso.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int pagina, [FromQuery] int tamanhoPagina, [FromQuery] string? busca,
        [FromQuery] FabricanteEquipamento? fabricante, [FromQuery] EstadoOperacionalEquipamento? estadoOperacional,
        [FromQuery] bool incluirRemovidos, CancellationToken cancellationToken)
    {
        var resultado = await _equipamentoAdminServico.ListarAsync(
            pagina, tamanhoPagina, busca, fabricante, estadoOperacional, incluirRemovidos, cancellationToken);
        return Ok(resultado);
    }

    /// <summary>Obtém um equipamento pelo Id.</summary>
    /// <response code="200">Equipamento encontrado.</response>
    /// <response code="404">Equipamento não encontrado.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _equipamentoAdminServico.ObterPorIdAsync(id, cancellationToken);
        if (!resultado.Success)
        {
            return NotFound(new { error = resultado.Error });
        }

        return Ok(resultado.Data);
    }

    /// <summary>Cadastra um novo equipamento. Número de Série deve ser único por Propriedade.</summary>
    /// <response code="201">Equipamento criado.</response>
    /// <response code="409">Propriedade inexistente ou Número de Série já cadastrado nesta propriedade.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CriarEquipamentoAdminRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _equipamentoAdminServico.CriarAsync(request, cancellationToken);
        if (!resultado.Success)
        {
            return Conflict(new { error = resultado.Error });
        }

        return StatusCode(StatusCodes.Status201Created, resultado.Data);
    }

    /// <summary>Atualiza os dados cadastrais de um equipamento (não altera o Estado Operacional — ver endpoint próprio).</summary>
    /// <response code="200">Equipamento atualizado.</response>
    /// <response code="404">Equipamento não encontrado, ou Número de Série já usado por outro equipamento nesta propriedade.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarEquipamentoAdminRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _equipamentoAdminServico.AtualizarAsync(id, request, cancellationToken);
        if (!resultado.Success)
        {
            return NotFound(new { error = resultado.Error });
        }

        return Ok(resultado.Data);
    }

    /// <summary>Altera o Estado Operacional (Ativo/EmManutencao/Inativo/Defeituoso) — decisão administrativa livre, sem máquina de estados.</summary>
    /// <response code="200">Estado atualizado.</response>
    /// <response code="404">Equipamento não encontrado.</response>
    [HttpPatch("{id:guid}/estado-operacional")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarEstadoOperacional(
        Guid id, [FromBody] AtualizarEstadoOperacionalRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _equipamentoAdminServico.AtualizarEstadoOperacionalAsync(id, request.EstadoOperacional, cancellationToken);
        if (!resultado.Success)
        {
            return NotFound(new { error = resultado.Error });
        }

        return Ok(resultado.Data);
    }

    /// <summary>Exclui (soft delete) um equipamento — nunca remove fisicamente (ADR 0009).</summary>
    /// <response code="204">Equipamento excluído.</response>
    /// <response code="404">Equipamento não encontrado.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var resultado = await _equipamentoAdminServico.ExcluirAsync(id, cancellationToken);
        if (!resultado.Success)
        {
            return NotFound(new { error = resultado.Error });
        }

        return NoContent();
    }
}
