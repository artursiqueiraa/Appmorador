using AppMorador.Api.Auth;
using AppMorador.Application.Cameras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 20 (ADR 0024) — List aninhado sob a Propriedade (mesmo padrão de Equipamentos/
/// Visitantes); demais ações usam só o Id da Câmera. Nenhum endpoint de
/// cadastro/edição/exclusão — Câmera não tem CRUD nesta Sprint (só visualização,
/// mission explícita).
/// </summary>
[ApiController]
[Authorize]
public sealed class CamerasController : ControllerBase
{
    private readonly ICameraServico _cameraServico;

    public CamerasController(ICameraServico cameraServico)
    {
        _cameraServico = cameraServico;
    }

    [HttpGet("api/properties/{propriedadeId:guid}/cameras")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _cameraServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>Metadados da última captura já salva — nunca dispara uma nova (ver POST abaixo).</summary>
    [HttpGet("api/cameras/{id:guid}/snapshot")]
    public async Task<IActionResult> ObterSnapshot(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cameraServico.ObterSnapshotAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return result.Data is null ? NoContent() : Ok(result.Data);
    }

    /// <summary>
    /// Captura sob demanda (botão "Atualizar imagem"). Sempre 200 — mesmo quando a
    /// captura falha, o corpo carrega <c>sucesso: false</c> + a última imagem ainda
    /// disponível (Fase 2.3 da missão); nunca 202 "processando", porque não existe
    /// canal assíncrono real por trás (a chamada ao gravador é síncrona, mesmo
    /// racional já usado para comandos JFL — ver ADR 0022 Decisão 10).
    /// </summary>
    [HttpPost("api/cameras/{id:guid}/snapshot")]
    public async Task<IActionResult> CapturarSnapshot(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cameraServico.CapturarSnapshotAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("api/cameras/{id:guid}/status")]
    public async Task<IActionResult> ObterStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cameraServico.ObterStatusAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    /// <summary>Bytes da imagem, autenticado + checagem de posse — nunca static files (a pasta de snapshots nunca é exposta publicamente).</summary>
    [HttpGet("api/cameras/{id:guid}/imagem")]
    public async Task<IActionResult> ObterImagem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cameraServico.ObterImagemAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return File(result.Data!.Conteudo, result.Data.ContentType);
    }
}
