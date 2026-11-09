using AppMorador.Api.Auth;
using AppMorador.Application.Moradores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Controllers;

/// <summary>Mesmo padrão de rota de <see cref="UnidadesController"/>: Create/List aninhados sob a Unidade, Update/Delete pelo Id do Morador.</summary>
[ApiController]
[Authorize]
public sealed class MoradoresController : ControllerBase
{
    private readonly IMoradorServico _moradorServico;
    private readonly IFotoMoradorServico _fotoMoradorServico;
    private readonly IConsentimentoBiometricoServico _consentimentos;
    private readonly IFacialLeitoresServico _leitores;

    public MoradoresController(
        IMoradorServico moradorServico, IFotoMoradorServico fotoMoradorServico, IConsentimentoBiometricoServico consentimentos, IFacialLeitoresServico leitores)
    {
        _consentimentos = consentimentos;
        _leitores = leitores;
        _moradorServico = moradorServico;
        _fotoMoradorServico = fotoMoradorServico;
    }

    [HttpPost("api/unidades/{unidadeId:guid}/moradores")]
    public async Task<IActionResult> Create(Guid unidadeId, [FromBody] CriarMoradorRequest request, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.CreateAsync(User.GetUsuarioId(), unidadeId, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    [HttpGet("api/unidades/{unidadeId:guid}/moradores")]
    public async Task<IActionResult> List(Guid unidadeId, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.ListByUnidadeAsync(User.GetUsuarioId(), unidadeId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPut("api/moradores/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AtualizarMoradorRequest request, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.UpdateAsync(User.GetUsuarioId(), id, request, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpDelete("api/moradores/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _moradorServico.DeleteAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Akteck Seg (ADR 0036) — envia/substitui a foto de rosto (multipart, campo "foto", JPG ou PNG,
    /// até 5 MB). O formato é conferido pelos bytes, nunca pelo nome do arquivo.
    /// </summary>
    [HttpPut("api/moradores/{id:guid}/foto")]
    [RequestSizeLimit(FotoMoradorServico.TamanhoMaximoBytes + 64 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DefinirFoto(Guid id, IFormFile? foto, CancellationToken cancellationToken)
    {
        if (foto is null || foto.Length == 0)
        {
            return BadRequest(new { error = "Envie a foto no campo \"foto\"." });
        }
        if (foto.Length > FotoMoradorServico.TamanhoMaximoBytes)
        {
            return BadRequest(new { error = "A foto precisa ter até 5 MB." });
        }

        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria, cancellationToken);
        var result = await _fotoMoradorServico.DefinirAsync(User.GetUsuarioId(), id, memoria.ToArray(), cancellationToken);
        if (!result.Success)
        {
            return result.Error == "Morador não encontrado." ? NotFound(new { error = result.Error }) : BadRequest(new { error = result.Error });
        }
        return NoContent();
    }

    /// <summary>Bytes da foto, autenticado + checagem de posse — a pasta de fotos nunca é exposta como arquivo estático.</summary>
    [HttpGet("api/moradores/{id:guid}/foto")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterFoto(Guid id, CancellationToken cancellationToken)
    {
        var result = await _fotoMoradorServico.ObterAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }
        Response.Headers.CacheControl = "private, no-store";
        return File(result.Data!.Conteudo, result.Data.TipoConteudo);
    }

    [HttpDelete("api/moradores/{id:guid}/foto")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoverFoto(Guid id, CancellationToken cancellationToken)
    {
        var result = await _fotoMoradorServico.RemoverAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(new { error = result.Error });
        }
        return NoContent();
    }

    /// <summary>Sprint 26 (ADR 0042) — consentimento biométrico (LGPD art. 11).</summary>
    [HttpGet("api/moradores/{id:guid}/consentimento-biometrico")]
    public async Task<IActionResult> ObterConsentimento(Guid id, CancellationToken cancellationToken)
    {
        var r = await _consentimentos.ObterAsync(User.GetUsuarioId(), id, cancellationToken);
        return r.Success ? Ok(r.Data) : NotFound(new { error = r.Error });
    }

    [HttpPost("api/moradores/{id:guid}/consentimento-biometrico")]
    public async Task<IActionResult> RegistrarConsentimento(Guid id, [FromBody] RegistrarConsentimentoRequest request, CancellationToken cancellationToken)
    {
        var r = await _consentimentos.RegistrarAsync(User.GetUsuarioId(), id, request, cancellationToken);
        return r.Success ? Ok(r.Data) : BadRequest(new { error = r.Error });
    }

    /// <summary>Revoga e apaga o rosto do servidor e dos leitores.</summary>
    [HttpDelete("api/moradores/{id:guid}/consentimento-biometrico")]
    public async Task<IActionResult> RevogarConsentimento(Guid id, CancellationToken cancellationToken)
    {
        var r = await _consentimentos.RevogarAsync(User.GetUsuarioId(), id, cancellationToken);
        return r.Success ? NoContent() : NotFound(new { error = r.Error });
    }

    /// <summary>Sprint 26 — situação do rosto em cada leitor facial ("Enviado", "Falhou").</summary>
    [HttpGet("api/moradores/{id:guid}/foto/leitores")]
    public async Task<IActionResult> SituacaoNosLeitores(Guid id, CancellationToken cancellationToken)
    {
        var consentimento = await _consentimentos.ObterAsync(User.GetUsuarioId(), id, cancellationToken);
        if (!consentimento.Success)
        {
            return NotFound(new { error = consentimento.Error });
        }

        return Ok(await _leitores.ListarAsync(id, cancellationToken));
    }
}
