using AppMorador.Application.Common;

namespace AppMorador.Application.Jfl;

/// <summary>Ações de comunicação real com uma central JFL já cadastrada como Equipamento (Fabricante=Jfl) — nunca CRUD (isso é <see cref="AppMorador.Application.Equipamentos.IEquipamentoServico"/>).</summary>
public interface IJflComandoServico
{
    Task<Result<CentralJflResponse>> ObterDetalhesAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<ResultadoTesteConexaoJfl>> TestarConexaoAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoJfl>> ConsultarStatusAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoJfl>> ArmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoJfl>> DesarmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoJfl>> ArmarStayAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoJfl>> ArmarAwayAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoJfl>> AcionarPgmAsync(Guid proprietarioId, Guid equipamentoId, int pgmNumero, CancellationToken cancellationToken);

    Task<Result<ResultadoComandoJfl>> DesligarPgmAsync(Guid proprietarioId, Guid equipamentoId, int pgmNumero, CancellationToken cancellationToken);

    /// <summary>Consulta o status atual, soma a zona ao conjunto já inibido, e reenvia o conjunto completo (protocolo não soma — ver ZoneInhibitCommandService).</summary>
    Task<Result<ResultadoComandoJfl>> InibirZonaAsync(Guid proprietarioId, Guid equipamentoId, int zonaNumero, CancellationToken cancellationToken);

    /// <summary>Consulta o status atual, remove a zona do conjunto inibido, e reenvia o conjunto completo.</summary>
    Task<Result<ResultadoComandoJfl>> DesinibirZonaAsync(Guid proprietarioId, Guid equipamentoId, int zonaNumero, CancellationToken cancellationToken);
}
