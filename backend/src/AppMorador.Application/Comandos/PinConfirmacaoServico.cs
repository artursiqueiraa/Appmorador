using AppMorador.Application.Autenticacao;
using AppMorador.Application.Common;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Comandos;

public sealed class SituacaoPinResponse
{
    public required bool Definido { get; init; }

    public DateTime? BloqueadoAteUtc { get; init; }
}

public sealed class DefinirPinRequest
{
    /// <summary>Senha da conta — exigida para criar, trocar ou redefinir ("esqueci o PIN").</summary>
    public string Senha { get; init; } = string.Empty;

    public string Pin { get; init; } = string.Empty;
}

public enum SituacaoValidacaoPin
{
    /// <summary>Usuário ainda não criou PIN: o comando segue sem confirmação (o app convida a criar).</summary>
    SemPinCadastrado,
    Valido,
    Ausente,
    Incorreto,
    Bloqueado,
}

public sealed record ResultadoValidacaoPin(SituacaoValidacaoPin Situacao, int? TentativasRestantes = null, DateTime? BloqueadoAteUtc = null);

public interface IPinConfirmacaoServico
{
    Task<Result<SituacaoPinResponse>> ObterAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<Result> DefinirAsync(Guid usuarioId, DefinirPinRequest request, CancellationToken cancellationToken);

    Task<ResultadoValidacaoPin> ValidarAsync(Guid usuarioId, string? pin, CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 27 (ADR 0043) — PIN de 4 a 6 dígitos que confirma desarmar e abrir. O servidor sempre
/// confere (a biometria do celular só libera o PIN guardado no aparelho); 5 erros seguidos
/// bloqueiam a confirmação por 15 minutos.
/// </summary>
public sealed class PinConfirmacaoServico : IPinConfirmacaoServico
{
    public const int MaximoTentativas = 5;
    public static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IPasswordHasher _hasher;

    public PinConfirmacaoServico(IUsuarioRepositorio usuarios, IPasswordHasher hasher)
    {
        _usuarios = usuarios;
        _hasher = hasher;
    }

    public async Task<Result<SituacaoPinResponse>> ObterAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (usuario is null)
        {
            return Result<SituacaoPinResponse>.Fail("Conta não encontrada.");
        }

        return Result<SituacaoPinResponse>.Ok(new SituacaoPinResponse
        {
            Definido = usuario.PinConfirmacaoHash is not null,
            BloqueadoAteUtc = usuario.PinBloqueadoAteUtc > DateTime.UtcNow ? usuario.PinBloqueadoAteUtc : null,
        });
    }

    public async Task<Result> DefinirAsync(Guid usuarioId, DefinirPinRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (usuario is null)
        {
            return Result.Fail("Conta não encontrada.");
        }

        if (string.IsNullOrEmpty(request.Senha) || !_hasher.Verify(request.Senha, usuario.SenhaHash))
        {
            return Result.Fail("Senha da conta incorreta.");
        }

        var erro = ValidarFormato(request.Pin);
        if (erro is not null)
        {
            return Result.Fail(erro);
        }

        usuario.PinConfirmacaoHash = _hasher.Hash(request.Pin);
        usuario.TentativasPinFalhas = 0;
        usuario.PinBloqueadoAteUtc = null;
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<ResultadoValidacaoPin> ValidarAsync(Guid usuarioId, string? pin, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (usuario?.PinConfirmacaoHash is null)
        {
            return new ResultadoValidacaoPin(SituacaoValidacaoPin.SemPinCadastrado);
        }

        if (usuario.PinBloqueadoAteUtc > DateTime.UtcNow)
        {
            return new ResultadoValidacaoPin(SituacaoValidacaoPin.Bloqueado, 0, usuario.PinBloqueadoAteUtc);
        }

        if (string.IsNullOrEmpty(pin))
        {
            return new ResultadoValidacaoPin(SituacaoValidacaoPin.Ausente);
        }

        if (_hasher.Verify(pin, usuario.PinConfirmacaoHash))
        {
            if (usuario.TentativasPinFalhas != 0 || usuario.PinBloqueadoAteUtc is not null)
            {
                usuario.TentativasPinFalhas = 0;
                usuario.PinBloqueadoAteUtc = null;
                await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return new ResultadoValidacaoPin(SituacaoValidacaoPin.Valido);
        }

        usuario.TentativasPinFalhas++;
        if (usuario.TentativasPinFalhas >= MaximoTentativas)
        {
            usuario.TentativasPinFalhas = 0;
            usuario.PinBloqueadoAteUtc = DateTime.UtcNow.Add(TempoBloqueio);
            await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ResultadoValidacaoPin(SituacaoValidacaoPin.Bloqueado, 0, usuario.PinBloqueadoAteUtc);
        }

        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ResultadoValidacaoPin(SituacaoValidacaoPin.Incorreto, MaximoTentativas - usuario.TentativasPinFalhas);
    }

    /// <summary>4 a 6 dígitos; recusa repetidos (0000) e sequências (1234, 4321).</summary>
    public static string? ValidarFormato(string? pin)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length is < 4 or > 6 || !pin.All(char.IsAsciiDigit))
        {
            return "O PIN precisa ter de 4 a 6 números.";
        }

        var repetido = pin.All(c => c == pin[0]);
        var crescente = Enumerable.Range(1, pin.Length - 1).All(i => pin[i] - pin[i - 1] == 1);
        var decrescente = Enumerable.Range(1, pin.Length - 1).All(i => pin[i] - pin[i - 1] == -1);
        return repetido || crescente || decrescente
            ? "Escolha um PIN menos óbvio (sem números repetidos ou em sequência)."
            : null;
    }
}
