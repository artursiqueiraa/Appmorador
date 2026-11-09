using System.Text.Json;
using AppMorador.Application.Common;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Comandos;

/// <summary>O que o app pediu, com a confirmação e a chave de idempotência vindas dos cabeçalhos.</summary>
public sealed class SolicitacaoComando
{
    public required Guid UsuarioId { get; init; }

    public required TipoComando Tipo { get; init; }

    public Guid? EquipamentoId { get; init; }

    public Guid? PontoAcessoId { get; init; }

    public string? Parametros { get; init; }

    public string? Pin { get; init; }

    /// <summary>O app informou que a digital/rosto do aparelho liberou o PIN.</summary>
    public bool ViaBiometria { get; init; }

    public string? ChaveIdempotencia { get; init; }
}

public enum DesfechoComando
{
    Executado,
    NaoEncontrado,
    ConfirmacaoNecessaria,
    EmAndamento,
}

public sealed class RespostaComando<T>
{
    public required DesfechoComando Desfecho { get; init; }

    public T? Dados { get; init; }

    public string? Erro { get; init; }

    /// <summary>PIN_OBRIGATORIO, PIN_INCORRETO ou PIN_BLOQUEADO.</summary>
    public string? CodigoConfirmacao { get; init; }

    public int? TentativasRestantes { get; init; }

    public DateTime? BloqueadoAteUtc { get; init; }
}

public interface IExecucaoComandoServico
{
    /// <summary>
    /// Confere acesso e confirmação, garante que a mesma chave não executa duas vezes, executa e
    /// registra em <see cref="ComandoExecutado"/>. <paramref name="avaliar"/> diz se o equipamento obedeceu.
    /// </summary>
    Task<RespostaComando<T>> ExecutarAsync<T>(
        SolicitacaoComando solicitacao,
        Func<CancellationToken, Task<Result<T>>> executar,
        Func<T, (bool Sucesso, string? Mensagem)> avaliar,
        CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 27 (ADR 0043) — porta única dos comandos que mudam o estado de um equipamento. Implementa
/// a parte da ADR 0033 que esta Sprint precisa — idempotência (sem retry automático de comando
/// destrutivo) e auditoria — e a confirmação por PIN em desarmar, acionar PGM e abrir.
/// </summary>
public sealed class ExecucaoComandoServico : IExecucaoComandoServico
{
    public static readonly TimeSpan JanelaIdempotencia = TimeSpan.FromHours(24);

    private static readonly HashSet<TipoComando> ExigemConfirmacao =
        [TipoComando.Desarmar, TipoComando.AcionarPgm, TipoComando.AbrirAcesso];

    private readonly IComandoExecutadoRepositorio _comandos;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IPontoAcessoRepositorio _pontosAcesso;
    private readonly IAcessoPropriedadeServico _acesso;
    private readonly IPinConfirmacaoServico _pin;

    public ExecucaoComandoServico(
        IComandoExecutadoRepositorio comandos,
        IEquipamentoRepositorio equipamentos,
        IPontoAcessoRepositorio pontosAcesso,
        IAcessoPropriedadeServico acesso,
        IPinConfirmacaoServico pin)
    {
        _comandos = comandos;
        _equipamentos = equipamentos;
        _pontosAcesso = pontosAcesso;
        _acesso = acesso;
        _pin = pin;
    }

    public static bool ExigeConfirmacao(TipoComando tipo) => ExigemConfirmacao.Contains(tipo);

    public async Task<RespostaComando<T>> ExecutarAsync<T>(
        SolicitacaoComando solicitacao,
        Func<CancellationToken, Task<Result<T>>> executar,
        Func<T, (bool Sucesso, string? Mensagem)> avaliar,
        CancellationToken cancellationToken)
    {
        var propriedadeId = await ResolverPropriedadeAsync(solicitacao, cancellationToken).ConfigureAwait(false);
        if (propriedadeId is null)
        {
            return new RespostaComando<T> { Desfecho = DesfechoComando.NaoEncontrado, Erro = "Equipamento não encontrado." };
        }

        var chave = NormalizarChave(solicitacao.ChaveIdempotencia);
        if (chave is not null)
        {
            var repetido = await RespostaJaRegistradaAsync<T>(solicitacao.UsuarioId, chave, cancellationToken).ConfigureAwait(false);
            if (repetido is not null)
            {
                return repetido;
            }
        }

        var confirmacao = FormaConfirmacao.Nenhuma;
        if (ExigeConfirmacao(solicitacao.Tipo))
        {
            var validacao = await _pin.ValidarAsync(solicitacao.UsuarioId, solicitacao.Pin, cancellationToken).ConfigureAwait(false);
            if (validacao.Situacao == SituacaoValidacaoPin.Valido)
            {
                confirmacao = solicitacao.ViaBiometria ? FormaConfirmacao.Biometria : FormaConfirmacao.Pin;
            }
            else if (validacao.Situacao != SituacaoValidacaoPin.SemPinCadastrado)
            {
                if (validacao.Situacao is SituacaoValidacaoPin.Incorreto or SituacaoValidacaoPin.Bloqueado)
                {
                    await RegistrarRecusaAsync(solicitacao, propriedadeId.Value, validacao, cancellationToken).ConfigureAwait(false);
                }

                return RecusaPorPin<T>(validacao);
            }
        }

        var registro = new ComandoExecutado
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId.Value,
            UsuarioId = solicitacao.UsuarioId,
            EquipamentoId = solicitacao.EquipamentoId,
            PontoAcessoId = solicitacao.PontoAcessoId,
            Tipo = solicitacao.Tipo,
            Parametros = solicitacao.Parametros,
            Confirmacao = confirmacao,
            ChaveIdempotencia = chave,
            Concluido = false,
            OcorridoEmUtc = DateTime.UtcNow,
        };

        if (!await _comandos.TentarReservarAsync(registro, cancellationToken).ConfigureAwait(false))
        {
            // Outra requisição com a mesma chave chegou primeiro (toque duplo simultâneo).
            return await RespostaJaRegistradaAsync<T>(solicitacao.UsuarioId, chave!, cancellationToken).ConfigureAwait(false)
                ?? new RespostaComando<T> { Desfecho = DesfechoComando.EmAndamento, Erro = "Esse comando já está sendo enviado." };
        }

        Result<T> resultado;
        try
        {
            resultado = await executar(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            registro.Sucesso = false;
            registro.Mensagem = "Erro inesperado ao enviar o comando.";
            registro.Concluido = true;
            await _comandos.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        registro.Concluido = true;
        if (!resultado.Success)
        {
            registro.Sucesso = false;
            registro.Mensagem = Cortar(resultado.Error);
            await _comandos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new RespostaComando<T> { Desfecho = DesfechoComando.NaoEncontrado, Erro = resultado.Error };
        }

        var (sucesso, mensagem) = avaliar(resultado.Data!);
        registro.Sucesso = sucesso;
        registro.Mensagem = sucesso ? null : Cortar(mensagem ?? "O equipamento não confirmou.");
        registro.RespostaJson = chave is null ? null : JsonSerializer.Serialize(resultado.Data);
        await _comandos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RespostaComando<T> { Desfecho = DesfechoComando.Executado, Dados = resultado.Data };
    }

    private static string? NormalizarChave(string? chave)
    {
        var limpa = chave?.Trim();
        return string.IsNullOrEmpty(limpa) ? null : limpa[..Math.Min(limpa.Length, 64)];
    }

    private async Task<Guid?> ResolverPropriedadeAsync(SolicitacaoComando solicitacao, CancellationToken cancellationToken)
    {
        Propriedade? propriedade = null;
        if (solicitacao.PontoAcessoId is { } pontoId)
        {
            propriedade = (await _pontosAcesso.GetByIdAsync(pontoId, cancellationToken).ConfigureAwait(false))?.Propriedade;
        }
        else if (solicitacao.EquipamentoId is { } equipamentoId)
        {
            propriedade = (await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false))?.Propriedade;
        }

        return propriedade is not null && _acesso.Pode(solicitacao.UsuarioId, propriedade, NivelAcesso.Operar) ? propriedade.Id : null;
    }

    private async Task<RespostaComando<T>?> RespostaJaRegistradaAsync<T>(Guid usuarioId, string chave, CancellationToken cancellationToken)
    {
        var anterior = await _comandos.GetPorChaveAsync(usuarioId, chave, DateTime.UtcNow - JanelaIdempotencia, cancellationToken).ConfigureAwait(false);
        if (anterior is null)
        {
            return null;
        }

        if (!anterior.Concluido)
        {
            return new RespostaComando<T> { Desfecho = DesfechoComando.EmAndamento, Erro = "Esse comando já está sendo enviado." };
        }

        if (anterior.RespostaJson is null)
        {
            return new RespostaComando<T> { Desfecho = DesfechoComando.NaoEncontrado, Erro = anterior.Mensagem ?? "Comando já processado." };
        }

        return new RespostaComando<T> { Desfecho = DesfechoComando.Executado, Dados = JsonSerializer.Deserialize<T>(anterior.RespostaJson) };
    }

    private async Task RegistrarRecusaAsync(SolicitacaoComando solicitacao, Guid propriedadeId, ResultadoValidacaoPin validacao, CancellationToken cancellationToken)
    {
        await _comandos.TentarReservarAsync(new ComandoExecutado
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            UsuarioId = solicitacao.UsuarioId,
            EquipamentoId = solicitacao.EquipamentoId,
            PontoAcessoId = solicitacao.PontoAcessoId,
            Tipo = solicitacao.Tipo,
            Parametros = solicitacao.Parametros,
            Confirmacao = FormaConfirmacao.Nenhuma,
            Sucesso = false,
            Mensagem = validacao.Situacao == SituacaoValidacaoPin.Bloqueado ? "PIN bloqueado após tentativas erradas." : "PIN incorreto.",
            Concluido = true,
            OcorridoEmUtc = DateTime.UtcNow,
        }, cancellationToken).ConfigureAwait(false);
    }

    private static RespostaComando<T> RecusaPorPin<T>(ResultadoValidacaoPin validacao) => validacao.Situacao switch
    {
        SituacaoValidacaoPin.Ausente => new RespostaComando<T>
        {
            Desfecho = DesfechoComando.ConfirmacaoNecessaria,
            CodigoConfirmacao = "PIN_OBRIGATORIO",
            Erro = "Confirme com o seu PIN.",
        },
        SituacaoValidacaoPin.Incorreto => new RespostaComando<T>
        {
            Desfecho = DesfechoComando.ConfirmacaoNecessaria,
            CodigoConfirmacao = "PIN_INCORRETO",
            TentativasRestantes = validacao.TentativasRestantes,
            Erro = validacao.TentativasRestantes == 1 ? "PIN incorreto. Resta 1 tentativa." : $"PIN incorreto. Restam {validacao.TentativasRestantes} tentativas.",
        },
        _ => new RespostaComando<T>
        {
            Desfecho = DesfechoComando.ConfirmacaoNecessaria,
            CodigoConfirmacao = "PIN_BLOQUEADO",
            BloqueadoAteUtc = validacao.BloqueadoAteUtc,
            Erro = "Muitas tentativas erradas. Aguarde 15 minutos ou redefina o PIN com a senha da conta.",
        },
    };

    private static string? Cortar(string? texto) => texto is null ? null : texto[..Math.Min(texto.Length, 200)];
}
