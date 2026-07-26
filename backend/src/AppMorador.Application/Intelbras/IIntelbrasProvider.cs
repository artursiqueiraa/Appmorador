namespace AppMorador.Application.Intelbras;

/// <summary>
/// Porta do Provider Intelbras — ÚNICO ponto do sistema que pode saber que o
/// protocolo é Intelbras. Segue a arquitetura OFICIAL de integração do AppMorador
/// (ADR 0014), com vocabulário de comando de central de alarme (ADR 0015), provando
/// que os dois eixos (direção de conexão, vocabulário de comando) são independentes
/// — ver ADR 0018.
/// </summary>
public interface IIntelbrasProvider
{
    Task<ResultadoTesteConexaoIntelbras> TestarConexaoAsync(ConexaoIntelbras conexao, CancellationToken cancellationToken);

    Task<ResultadoComandoIntelbras> ConsultarStatusAsync(ConexaoIntelbras conexao, CancellationToken cancellationToken);

    Task<ResultadoComandoIntelbras> ArmarAsync(ConexaoIntelbras conexao, int particao, CancellationToken cancellationToken);

    Task<ResultadoComandoIntelbras> DesarmarAsync(ConexaoIntelbras conexao, int particao, CancellationToken cancellationToken);

    /// <summary>Importação sob demanda (mesmo padrão de Control iD/ADR 0014) — nunca um poller em background.</summary>
    Task<IReadOnlyList<EventoImportadoIntelbras>> ImportarEventosAsync(ConexaoIntelbras conexao, CancellationToken cancellationToken);
}
