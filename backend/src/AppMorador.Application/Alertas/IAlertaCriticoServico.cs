using AppMorador.Application.Common;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Alertas;

/// <summary>
/// Sprint 24 (ADR 0040) — ciclo de vida do alerta crítico: abrir (central, pânico, vigia de
/// conexão), avisar o dono, confirmar ("Estou ciente") e escalonar quando ninguém confirma.
/// Único lugar com a regra de quem é avisado em cada nível.
/// </summary>
public interface IAlertaCriticoServico
{
    /// <summary>
    /// Abre o alerta e avisa o dono. Se já houver um alerta aberto do mesmo tipo na propriedade,
    /// devolve esse (sem notificar de novo) — disparos repetidos não viram dezenas de alertas.
    /// </summary>
    Task<AlertaCriticoResponse?> CriarAsync(NovoAlertaCritico novo, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AlertaCriticoResponse>>> ListarAsync(Guid usuarioId, Guid propriedadeId, bool apenasAbertos, CancellationToken cancellationToken);

    Task<Result<AlertaCriticoResponse>> ObterAsync(Guid usuarioId, Guid alertaId, CancellationToken cancellationToken);

    /// <summary>"Estou ciente": registra quem e quando e encerra o escalonamento. Idempotente.</summary>
    Task<Result<AlertaCriticoResponse>> ConfirmarAsync(Guid usuarioId, Guid alertaId, CancellationToken cancellationToken);

    /// <summary>Botão de pânico do app — mesmo fluxo de um disparo, com o autor registrado.</summary>
    Task<Result<AlertaCriticoResponse>> AcionarPanicoAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>
    /// Para de escalonar o alerta aberto daquele tipo (ex.: a central voltou a se comunicar). O alerta
    /// continua aberto até alguém confirmar; só deixa de avisar mais gente.
    /// </summary>
    Task EncerrarEscalonamentoAsync(Guid propriedadeId, TipoAlertaCritico tipo, CancellationToken cancellationToken);

    /// <summary>Avança os alertas vencidos para o próximo nível. Devolve quantos avançaram.</summary>
    Task<int> EscalonarVencidosAsync(DateTime agoraUtc, CancellationToken cancellationToken);

    Task<Result<ConfiguracaoAlertasResponse>> ObterConfiguracaoAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<ConfiguracaoAlertasResponse>> AtualizarConfiguracaoAsync(
        Guid usuarioId, Guid propriedadeId, AtualizarConfiguracaoAlertasRequest request, CancellationToken cancellationToken);
}
