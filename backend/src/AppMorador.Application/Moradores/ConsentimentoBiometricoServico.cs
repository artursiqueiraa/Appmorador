using AppMorador.Application.Common;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Moradores;

public sealed class ConsentimentoResponse
{
    public required bool Ativo { get; init; }

    public string? VersaoTermo { get; init; }

    public DateTime? RegistradoEmUtc { get; init; }

    public bool ResponsavelLegal { get; init; }

    /// <summary>true quando a pessoa é Dependente: o app pede a confirmação de responsável legal.</summary>
    public required bool ExigeResponsavel { get; init; }

    public required string VersaoTermoAtual { get; init; }
}

public sealed class RegistrarConsentimentoRequest
{
    /// <summary>Obrigatório para Dependente: quem registra declara ser o responsável legal.</summary>
    public bool ResponsavelLegal { get; init; }
}

public interface IConsentimentoBiometricoServico
{
    Task<Result<ConsentimentoResponse>> ObterAsync(Guid usuarioId, Guid moradorId, CancellationToken cancellationToken);

    Task<Result<ConsentimentoResponse>> RegistrarAsync(Guid usuarioId, Guid moradorId, RegistrarConsentimentoRequest request, CancellationToken cancellationToken);

    /// <summary>Revoga e apaga a foto do servidor e dos leitores (LGPD art. 8º §5º).</summary>
    Task<Result> RevogarAsync(Guid usuarioId, Guid moradorId, CancellationToken cancellationToken);
}

/// <summary>Sprint 26 (ADR 0042) — consentimento biométrico por pessoa (LGPD art. 11).</summary>
public sealed class ConsentimentoBiometricoServico : IConsentimentoBiometricoServico
{
    private readonly IMoradorRepositorio _moradores;
    private readonly IConsentimentoBiometricoRepositorio _consentimentos;
    private readonly IFotoMoradorServico _fotos;
    private readonly IAcessoPropriedadeServico _acesso;

    public ConsentimentoBiometricoServico(
        IMoradorRepositorio moradores, IConsentimentoBiometricoRepositorio consentimentos, IFotoMoradorServico fotos, IAcessoPropriedadeServico acesso)
    {
        _moradores = moradores;
        _consentimentos = consentimentos;
        _fotos = fotos;
        _acesso = acesso;
    }

    public async Task<Result<ConsentimentoResponse>> ObterAsync(Guid usuarioId, Guid moradorId, CancellationToken cancellationToken)
    {
        var morador = await ObterAsync(usuarioId, moradorId, NivelAcesso.Ver, cancellationToken).ConfigureAwait(false);
        if (morador is null)
        {
            return Result<ConsentimentoResponse>.Fail("Pessoa não encontrada.");
        }

        var ativo = await _consentimentos.GetAtivoAsync(moradorId, cancellationToken).ConfigureAwait(false);
        return Result<ConsentimentoResponse>.Ok(Montar(morador, ativo));
    }

    public async Task<Result<ConsentimentoResponse>> RegistrarAsync(
        Guid usuarioId, Guid moradorId, RegistrarConsentimentoRequest request, CancellationToken cancellationToken)
    {
        var morador = await ObterAsync(usuarioId, moradorId, NivelAcesso.Alterar, cancellationToken).ConfigureAwait(false);
        if (morador is null)
        {
            return Result<ConsentimentoResponse>.Fail("Pessoa não encontrada.");
        }

        if (morador.Tipo == TipoMorador.Dependente && !request.ResponsavelLegal)
        {
            return Result<ConsentimentoResponse>.Fail("Para dependentes, confirme que você é o responsável legal.");
        }

        var ativo = await _consentimentos.GetAtivoAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (ativo is not null)
        {
            ativo.RevogadoEmUtc = DateTime.UtcNow; // substituído pela versão atual do termo
        }

        var novo = new ConsentimentoBiometrico
        {
            Id = Guid.NewGuid(),
            MoradorId = moradorId,
            RegistradoPorUsuarioId = usuarioId,
            VersaoTermo = ConsentimentoBiometrico.VersaoTermoAtual,
            ResponsavelLegal = request.ResponsavelLegal,
            RegistradoEmUtc = DateTime.UtcNow,
        };
        await _consentimentos.AddAsync(novo, cancellationToken).ConfigureAwait(false);
        await _consentimentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ConsentimentoResponse>.Ok(Montar(morador, novo));
    }

    public async Task<Result> RevogarAsync(Guid usuarioId, Guid moradorId, CancellationToken cancellationToken)
    {
        var morador = await ObterAsync(usuarioId, moradorId, NivelAcesso.Alterar, cancellationToken).ConfigureAwait(false);
        if (morador is null)
        {
            return Result.Fail("Pessoa não encontrada.");
        }

        var ativo = await _consentimentos.GetAtivoAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (ativo is not null)
        {
            ativo.RevogadoEmUtc = DateTime.UtcNow;
            await _consentimentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Sem consentimento não há base para manter o rosto: apaga do servidor e dos leitores.
        return await _fotos.RemoverAsync(usuarioId, moradorId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Morador?> ObterAsync(Guid usuarioId, Guid moradorId, NivelAcesso nivel, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        return morador?.Unidade?.Propriedade is not null && _acesso.Pode(usuarioId, morador.Unidade.Propriedade, nivel) ? morador : null;
    }

    private static ConsentimentoResponse Montar(Morador morador, ConsentimentoBiometrico? c) => new()
    {
        Ativo = c is not null,
        VersaoTermo = c?.VersaoTermo,
        RegistradoEmUtc = c?.RegistradoEmUtc,
        ResponsavelLegal = c?.ResponsavelLegal ?? false,
        ExigeResponsavel = morador.Tipo == TipoMorador.Dependente,
        VersaoTermoAtual = ConsentimentoBiometrico.VersaoTermoAtual,
    };
}
