using AppMorador.Api.Auth;
using AppMorador.Application.Comandos;
using AppMorador.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AppMorador.Api.Comandos;

/// <summary>
/// Sprint 27 (ADR 0043) — tradução HTTP dos comandos auditados. A confirmação e a chave de
/// idempotência vêm em cabeçalhos para não mudar o corpo das rotas já existentes:
/// <c>X-Confirmacao-Pin</c>, <c>X-Confirmacao-Biometria: 1</c> e <c>Idempotency-Key</c>.
/// </summary>
public static class ComandoHttp
{
    public const string CabecalhoPin = "X-Confirmacao-Pin";
    public const string CabecalhoBiometria = "X-Confirmacao-Biometria";
    public const string CabecalhoIdempotencia = "Idempotency-Key";

    public static SolicitacaoComando Solicitacao(
        this ControllerBase controller, TipoComando tipo, Guid? equipamentoId = null, Guid? pontoAcessoId = null, string? parametros = null)
    {
        var cabecalhos = controller.Request.Headers;
        return new SolicitacaoComando
        {
            UsuarioId = controller.User.GetUsuarioId(),
            Tipo = tipo,
            EquipamentoId = equipamentoId,
            PontoAcessoId = pontoAcessoId,
            Parametros = parametros,
            Pin = cabecalhos[CabecalhoPin].FirstOrDefault(),
            ViaBiometria = cabecalhos[CabecalhoBiometria].FirstOrDefault() == "1",
            ChaveIdempotencia = cabecalhos[CabecalhoIdempotencia].FirstOrDefault(),
        };
    }

    /// <summary>200 com os dados; 404; 428 quando falta ou erra a confirmação; 409 quando a mesma chave ainda está em execução.</summary>
    public static IActionResult ParaResposta<T>(this ControllerBase controller, RespostaComando<T> resposta) => resposta.Desfecho switch
    {
        DesfechoComando.Executado => controller.Ok(resposta.Dados),
        DesfechoComando.ConfirmacaoNecessaria => controller.StatusCode(StatusCodes.Status428PreconditionRequired, new
        {
            error = resposta.Erro,
            codigo = resposta.CodigoConfirmacao,
            tentativasRestantes = resposta.TentativasRestantes,
            bloqueadoAteUtc = resposta.BloqueadoAteUtc,
        }),
        DesfechoComando.EmAndamento => controller.Conflict(new { error = resposta.Erro }),
        _ => controller.NotFound(new { error = resposta.Erro }),
    };
}
