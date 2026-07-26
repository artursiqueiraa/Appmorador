using AppMorador.Api.Auth;
using AppMorador.Application.Operacional;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Sprint 13 — Camada Operacional Unificada (ADR 0016). Única porta pela qual
/// Dashboard/Mobile obtêm o Snapshot Operacional — nunca consultam
/// IControlIdProvider/IJflProvider diretamente. A Timeline Operacional (Central de
/// Eventos já existente, ver ADR 0006) continua exposta por
/// <see cref="EventosController"/> — reaproveitada, nunca duplicada aqui.
/// </summary>
[ApiController]
[Authorize]
public sealed class OperacionalController : ControllerBase
{
    private readonly ISnapshotOperacionalServico _snapshotOperacionalServico;

    public OperacionalController(ISnapshotOperacionalServico snapshotOperacionalServico)
    {
        _snapshotOperacionalServico = snapshotOperacionalServico;
    }

    [HttpGet("api/properties/{propriedadeId:guid}/operacional/snapshot")]
    public async Task<IActionResult> ObterSnapshot(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _snapshotOperacionalServico.ObterAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/properties/{propriedadeId:guid}/operacional/snapshot/atualizar")]
    public async Task<IActionResult> AtualizarSnapshot(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _snapshotOperacionalServico.AtualizarAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
