using AppMorador.Application.Jfl;
using AppMorador.Jfl.Messages.Status;
using AppMorador.Jfl.Server;

namespace AppMorador.Infrastructure.Jfl;

/// <summary>
/// Única implementação real de <see cref="IJflProvider"/> — nunca disca para o
/// equipamento (a central é quem abre a conexão TCP, ver JflTcpServer/SessionManager
/// em AppMorador.Jfl); cada método localiza a sessão já registrada pelo número de
/// série e envia o comando dentro dela via <see cref="CentralStatusQueryService"/>/
/// <see cref="ArmCommandService"/>/<see cref="PgmCommandService"/>/
/// <see cref="ZoneInhibitCommandService"/> (Sprint 12, ADR 0014/0015).
/// </summary>
internal sealed class JflProvider : IJflProvider
{
    private static readonly TimeSpan TimeoutTeste = TimeSpan.FromSeconds(5);

    private readonly SessionManager _sessionManager;
    private readonly CentralStatusQueryService _statusQuery;
    private readonly ArmCommandService _armComando;
    private readonly PgmCommandService _pgmComando;
    private readonly ZoneInhibitCommandService _zoneInhibitComando;

    public JflProvider(
        SessionManager sessionManager,
        CentralStatusQueryService statusQuery,
        ArmCommandService armComando,
        PgmCommandService pgmComando,
        ZoneInhibitCommandService zoneInhibitComando)
    {
        _sessionManager = sessionManager;
        _statusQuery = statusQuery;
        _armComando = armComando;
        _pgmComando = pgmComando;
        _zoneInhibitComando = zoneInhibitComando;
    }

    public async Task<ResultadoTesteConexaoJfl> TestarConexaoAsync(string numeroSerie, CancellationToken cancellationToken)
    {
        if (!_sessionManager.TryGet(numeroSerie, out _))
        {
            return new ResultadoTesteConexaoJfl { Sucesso = false, MensagemErro = "Central não possui conexão ativa (offline)." };
        }

        var resultado = await _statusQuery.ConsultarAsync(numeroSerie, cancellationToken, TimeoutTeste).ConfigureAwait(false);
        return new ResultadoTesteConexaoJfl { Sucesso = resultado.Sucesso, MensagemErro = resultado.Erro };
    }

    public async Task<ResultadoComandoJfl> ConsultarStatusAsync(string numeroSerie, CancellationToken cancellationToken)
    {
        var resultado = await _statusQuery.ConsultarAsync(numeroSerie, cancellationToken).ConfigureAwait(false);
        return resultado.Sucesso
            ? new ResultadoComandoJfl { Sucesso = true, StatusResultante = JflStatusMapper.ParaStatusCentralJflInfo(resultado.Status!) }
            : new ResultadoComandoJfl { Sucesso = false, MensagemErro = resultado.Erro };
    }

    public async Task<ResultadoComandoJfl> ArmarAsync(string numeroSerie, int particao, CancellationToken cancellationToken)
    {
        var resultado = await _armComando.ArmarAsync(numeroSerie, particao, cancellationToken).ConfigureAwait(false);
        return ParaResultadoComando(resultado.Sucesso, resultado.Erro, resultado.StatusResultante);
    }

    public async Task<ResultadoComandoJfl> DesarmarAsync(string numeroSerie, int particao, CancellationToken cancellationToken)
    {
        var resultado = await _armComando.DesarmarAsync(numeroSerie, particao, cancellationToken).ConfigureAwait(false);
        return ParaResultadoComando(resultado.Sucesso, resultado.Erro, resultado.StatusResultante);
    }

    public async Task<ResultadoComandoJfl> ArmarStayAsync(string numeroSerie, int particao, CancellationToken cancellationToken)
    {
        var resultado = await _armComando.ArmarStayAsync(numeroSerie, particao, cancellationToken).ConfigureAwait(false);
        return ParaResultadoComando(resultado.Sucesso, resultado.Erro, resultado.StatusResultante);
    }

    public async Task<ResultadoComandoJfl> ArmarAwayAsync(string numeroSerie, int particao, CancellationToken cancellationToken)
    {
        var resultado = await _armComando.ArmarAwayAsync(numeroSerie, particao, cancellationToken).ConfigureAwait(false);
        return ParaResultadoComando(resultado.Sucesso, resultado.Erro, resultado.StatusResultante);
    }

    public async Task<ResultadoComandoJfl> AcionarPgmAsync(string numeroSerie, int pgmNumero, CancellationToken cancellationToken)
    {
        var resultado = await _pgmComando.AcionarAsync(numeroSerie, pgmNumero, cancellationToken).ConfigureAwait(false);
        return ParaResultadoComando(resultado.Sucesso, resultado.Erro, resultado.StatusResultante);
    }

    public async Task<ResultadoComandoJfl> DesligarPgmAsync(string numeroSerie, int pgmNumero, CancellationToken cancellationToken)
    {
        var resultado = await _pgmComando.DesacionarAsync(numeroSerie, pgmNumero, cancellationToken).ConfigureAwait(false);
        return ParaResultadoComando(resultado.Sucesso, resultado.Erro, resultado.StatusResultante);
    }

    public async Task<ResultadoComandoJfl> InibirZonasAsync(
        string numeroSerie, IReadOnlySet<int> zonasQueDevemFicarInibidas, CancellationToken cancellationToken)
    {
        var resultado = await _zoneInhibitComando.InibirZonasAsync(numeroSerie, zonasQueDevemFicarInibidas, cancellationToken).ConfigureAwait(false);
        return ParaResultadoComando(resultado.Sucesso, resultado.Erro, resultado.StatusResultante);
    }

    private static ResultadoComandoJfl ParaResultadoComando(
        bool sucesso, string? erro, CentralStatusResponse? statusResultante) => new()
    {
        Sucesso = sucesso,
        MensagemErro = erro,
        StatusResultante = statusResultante is not null ? JflStatusMapper.ParaStatusCentralJflInfo(statusResultante) : null,
    };
}
