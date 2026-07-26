using AppMorador.Api.Auth;
using AppMorador.Application.Equipamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>
/// Create/List aninhados sob a Propriedade (mesmo padrão de PontoAcesso/Vaga/Visitante
/// — Equipamento pertence direto à Propriedade); demais ações usam só o Id do
/// Equipamento. Ações de integração real (testar-conexao/informacoes/sincronizar-*/
/// importar-eventos) vivem aqui também, mas delegam a <see cref="IEquipamentoIntegracaoServico"/>
/// — nunca a <see cref="IEquipamentoServico"/> (que é só CRUD), ver ADR 0014.
/// </summary>
[ApiController]
[Authorize]
public sealed class EquipamentosController : ControllerBase
{
    private readonly IEquipamentoServico _equipamentoServico;
    private readonly IEquipamentoIntegracaoServico _integracaoServico;

    public EquipamentosController(IEquipamentoServico equipamentoServico, IEquipamentoIntegracaoServico integracaoServico)
    {
        _equipamentoServico = equipamentoServico;
        _integracaoServico = integracaoServico;
    }

    [HttpPost("api/properties/{propriedadeId:guid}/equipamentos")]
    public async Task<IActionResult> Create(Guid propriedadeId, [FromBody] CriarEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var result = await _equipamentoServico.CreateAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/properties/{propriedadeId:guid}/equipamentos")]
    public async Task<IActionResult> List(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var result = await _equipamentoServico.ListByPropriedadeAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("api/equipamentos/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _equipamentoServico.GetByIdAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/equipamentos/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var result = await _equipamentoServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/equipamentos/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _equipamentoServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }

    [HttpPost("api/equipamentos/{id:guid}/testar-conexao")]
    public async Task<IActionResult> TestarConexao(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integracaoServico.TestarConexaoAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("api/equipamentos/{id:guid}/informacoes")]
    public async Task<IActionResult> ConsultarInformacoes(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integracaoServico.ConsultarInformacoesAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/sincronizar-moradores")]
    public async Task<IActionResult> SincronizarMoradores(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integracaoServico.SincronizarMoradoresAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/sincronizar-credenciais")]
    public async Task<IActionResult> SincronizarCredenciais(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integracaoServico.SincronizarCredenciaisAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/sincronizar-permissoes")]
    public async Task<IActionResult> SincronizarPermissoes(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integracaoServico.SincronizarPermissoesAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("api/equipamentos/{id:guid}/importar-eventos")]
    public async Task<IActionResult> ImportarEventos(Guid id, CancellationToken cancellationToken)
    {
        var result = await _integracaoServico.ImportarEventosAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }
}
