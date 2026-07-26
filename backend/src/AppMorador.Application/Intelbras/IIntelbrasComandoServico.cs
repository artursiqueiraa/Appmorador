using AppMorador.Application.Common;

namespace AppMorador.Application.Intelbras;

/// <summary>
/// Orquestra a comunicação real com uma central Intelbras: resolve o Equipamento
/// (Fabricante=Intelbras), decifra a senha, delega a <see cref="IIntelbrasProvider"/>,
/// e persiste eventos importados no domínio de Eventos já existente
/// (<see cref="AppMorador.Domain.Entities.EventoEquipamento"/>, reaproveitado sem
/// alteração). Nunca conhece o protocolo Intelbras por dentro — isso é exclusividade
/// do Provider (ADR 0014/0018).
/// </summary>
public interface IIntelbrasComandoServico
{
    Task<Result<CentralIntelbrasResponse>> ObterDetalhesAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<ResultadoTesteConexaoIntelbras>> TestarConexaoAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoIntelbras>> ConsultarStatusAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoIntelbras>> ArmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoIntelbras>> DesarmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken);

    Task<Result<ImportacaoEventosIntelbrasResponse>> ImportarEventosAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);
}
