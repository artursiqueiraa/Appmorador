using AppMorador.Application.Rbac;
using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Moradores;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Moradores;

public interface IFotoMoradorServico
{
    Task<Result> DefinirAsync(Guid proprietarioId, Guid moradorId, byte[] conteudo, CancellationToken cancellationToken);

    Task<Result<FotoMorador>> ObterAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken);

    Task<Result> RemoverAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken);
}

public sealed record FotoMorador(Stream Conteudo, string TipoConteudo);

/// <summary>
/// Akteck Seg (ADR 0036) — foto de rosto do morador salva no servidor (antes só existia
/// como miniatura no aparelho, ADR 0020). Mesma regra de posse de <see cref="MoradorServico"/>
/// (Morador → Unidade → Propriedade.ProprietarioId) e mesma mensagem para "não existe" e
/// "não é seu". O formato é conferido pelos bytes do arquivo, nunca pelo nome ou pelo
/// Content-Type informado pelo cliente.
/// </summary>
public sealed class FotoMoradorServico : IFotoMoradorServico
{
    private readonly IAcessoPropriedadeServico _acesso;

    public const int TamanhoMaximoBytes = 5 * 1024 * 1024;
    private const string NaoEncontrado = "Morador não encontrado.";

    private readonly IMoradorRepositorio _moradores;
    private readonly IArmazenamentoFotoMorador _armazenamento;
    private readonly IConsentimentoBiometricoRepositorio _consentimentos;
    private readonly IFacialLeitoresServico _leitores;

    public FotoMoradorServico(IMoradorRepositorio moradores, IArmazenamentoFotoMorador armazenamento,
        IAcessoPropriedadeServico acesso, IConsentimentoBiometricoRepositorio consentimentos, IFacialLeitoresServico leitores)
    {
        _acesso = acesso;
        _consentimentos = consentimentos;
        _leitores = leitores;
        _moradores = moradores;
        _armazenamento = armazenamento;
    }

    public async Task<Result> DefinirAsync(Guid proprietarioId, Guid moradorId, byte[] conteudo, CancellationToken cancellationToken)
    {
        if (conteudo.Length == 0 || conteudo.Length > TamanhoMaximoBytes)
        {
            return Result.Fail("A foto precisa ter até 5 MB.");
        }
        var formato = DetectarFormato(conteudo);
        if (formato is null)
        {
            return Result.Fail("Envie uma foto em JPG ou PNG.");
        }

        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAo(morador, proprietarioId))
        {
            return Result.Fail(NaoEncontrado);
        }

        // Sprint 26 (ADR 0042, LGPD art. 11) — rosto só com consentimento ativo; dependente exige
        // a confirmação de responsável legal.
        var consentimento = await _consentimentos.GetAtivoAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (consentimento is null)
        {
            return Result.Fail("Registre o consentimento da pessoa antes de cadastrar o rosto.");
        }

        if (morador!.Tipo == TipoMorador.Dependente && !consentimento.ResponsavelLegal)
        {
            return Result.Fail("Para dependentes, o consentimento precisa ser do responsável legal.");
        }

        var anterior = morador.FotoPath;
        morador.FotoPath = await _armazenamento.SalvarAsync(moradorId, conteudo, formato.Value.Extensao, cancellationToken).ConfigureAwait(false);
        await _moradores.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(anterior))
        {
            _armazenamento.Remover(anterior);
        }

        await _leitores.SincronizarAsync(morador, conteudo, cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<FotoMorador>> ObterAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAo(morador, proprietarioId, NivelAcesso.Ver) || string.IsNullOrEmpty(morador!.FotoPath))
        {
            return Result<FotoMorador>.Fail(NaoEncontrado);
        }
        var arquivo = _armazenamento.Abrir(morador.FotoPath);
        if (arquivo is null)
        {
            return Result<FotoMorador>.Fail(NaoEncontrado);
        }
        var tipo = morador.FotoPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
        return Result<FotoMorador>.Ok(new FotoMorador(arquivo, tipo));
    }

    public async Task<Result> RemoverAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAo(morador, proprietarioId))
        {
            return Result.Fail(NaoEncontrado);
        }
        if (!string.IsNullOrEmpty(morador!.FotoPath))
        {
            var caminho = morador.FotoPath;
            morador.FotoPath = null;
            await _moradores.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _armazenamento.Remover(caminho);
        }

        // Sprint 26 — também some dos leitores (LGPD arts. 15–16).
        await _leitores.SincronizarAsync(morador!, null, cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <summary>JPEG começa com FF D8 FF; PNG com 89 50 4E 47 0D 0A 1A 0A.</summary>
    internal static (string Extensao, string TipoConteudo)? DetectarFormato(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
        {
            return ("jpg", "image/jpeg");
        }
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
        {
            return ("png", "image/png");
        }
        return null;
    }

    private bool PertenceAo(Morador? morador, Guid proprietarioId, NivelAcesso nivel = NivelAcesso.Alterar) =>
        morador is not null && morador.Unidade?.Propriedade is not null && _acesso.Pode(proprietarioId, morador.Unidade.Propriedade, nivel);
}
