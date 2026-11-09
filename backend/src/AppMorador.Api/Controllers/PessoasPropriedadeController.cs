using AppMorador.Api.Auth;
using AppMorador.Application.Conta;
using AppMorador.Application.Pessoas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Sprint 25 (ADR 0041) — pessoas com acesso, convites e a própria conta.</summary>
[ApiController]
[Authorize]
public sealed class PessoasPropriedadeController : ControllerBase
{
    private readonly IPessoasPropriedadeServico _pessoas;
    private readonly IMinhaContaServico _conta;

    public PessoasPropriedadeController(IPessoasPropriedadeServico pessoas, IMinhaContaServico conta)
    {
        _pessoas = pessoas;
        _conta = conta;
    }

    [HttpGet("api/properties/{propriedadeId:guid}/pessoas")]
    public async Task<IActionResult> Listar(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var r = await _pessoas.ListarAsync(User.GetUsuarioId(), propriedadeId, cancellationToken);
        return r.Success ? Ok(r.Data) : NotFound(new { error = r.Error });
    }

    [HttpPost("api/properties/{propriedadeId:guid}/convites")]
    public async Task<IActionResult> Convidar(Guid propriedadeId, [FromBody] ConvidarPessoaRequest request, CancellationToken cancellationToken)
    {
        var r = await _pessoas.ConvidarAsync(User.GetUsuarioId(), propriedadeId, request, cancellationToken);
        if (r.Success)
        {
            return StatusCode(StatusCodes.Status201Created, r.Data);
        }

        return r.Error == "Propriedade não encontrada." ? NotFound(new { error = r.Error }) : BadRequest(new { error = r.Error });
    }

    [HttpDelete("api/convites/{id:guid}")]
    public async Task<IActionResult> Revogar(Guid id, CancellationToken cancellationToken)
    {
        var r = await _pessoas.RevogarConviteAsync(User.GetUsuarioId(), id, cancellationToken);
        return r.Success ? NoContent() : NotFound(new { error = r.Error });
    }

    [HttpPut("api/pessoas/{vinculoId:guid}/papel")]
    public async Task<IActionResult> AlterarPapel(Guid vinculoId, [FromBody] AlterarPapelRequest request, CancellationToken cancellationToken)
    {
        var r = await _pessoas.AlterarPapelAsync(User.GetUsuarioId(), vinculoId, request, cancellationToken);
        return r.Success ? NoContent() : BadRequest(new { error = r.Error });
    }

    [HttpDelete("api/pessoas/{vinculoId:guid}")]
    public async Task<IActionResult> Remover(Guid vinculoId, CancellationToken cancellationToken)
    {
        var r = await _pessoas.RemoverAsync(User.GetUsuarioId(), vinculoId, cancellationToken);
        return r.Success ? NoContent() : BadRequest(new { error = r.Error });
    }

    /// <summary>Tela de aceite: mostra de quem é o convite antes de entrar/criar conta.</summary>
    [AllowAnonymous]
    [HttpGet("api/convites/codigo/{codigo}")]
    public async Task<IActionResult> ObterConvite(string codigo, CancellationToken cancellationToken) =>
        Ok(await _pessoas.ObterConviteAsync(codigo, cancellationToken));

    [HttpPost("api/convites/codigo/{codigo}/aceitar")]
    public async Task<IActionResult> Aceitar(string codigo, CancellationToken cancellationToken)
    {
        var r = await _pessoas.AceitarConviteAsync(User.GetUsuarioId(), codigo, cancellationToken);
        return r.Success ? Ok(new { propriedadeId = r.Data }) : BadRequest(new { error = r.Error });
    }

    [HttpGet("api/minha-conta")]
    public async Task<IActionResult> ObterConta(CancellationToken cancellationToken)
    {
        var r = await _conta.ObterAsync(User.GetUsuarioId(), cancellationToken);
        return r.Success ? Ok(r.Data) : NotFound(new { error = r.Error });
    }

    [HttpPut("api/minha-conta")]
    public async Task<IActionResult> AtualizarConta([FromBody] AtualizarMinhaContaRequest request, CancellationToken cancellationToken)
    {
        var r = await _conta.AtualizarAsync(User.GetUsuarioId(), request, cancellationToken);
        return r.Success ? Ok(r.Data) : BadRequest(new { error = r.Error });
    }

    [HttpPut("api/minha-conta/senha")]
    public async Task<IActionResult> TrocarSenha([FromBody] TrocarSenhaRequest request, CancellationToken cancellationToken)
    {
        var r = await _conta.TrocarSenhaAsync(User.GetUsuarioId(), request, cancellationToken);
        return r.Success ? NoContent() : BadRequest(new { error = r.Error });
    }

    [HttpPost("api/minha-conta/excluir")]
    public async Task<IActionResult> ExcluirConta([FromBody] ExcluirContaRequest request, CancellationToken cancellationToken)
    {
        var r = await _conta.ExcluirAsync(User.GetUsuarioId(), request, cancellationToken);
        return r.Success ? NoContent() : BadRequest(new { error = r.Error });
    }
}
