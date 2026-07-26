namespace AppMorador.Application.Jfl;

/// <summary>
/// Porta do Provider JFL Active 100 Bus — ÚNICO ponto do sistema que pode saber que
/// o protocolo é JFL (mesmo papel de <see cref="AppMorador.Application.ControlId.IControlIdProvider"/>,
/// ver ADR 0014/0015). Diferença estrutural importante em relação ao Control iD:
/// aqui o AppMorador nunca disca para o equipamento — a central é quem abre e
/// mantém a conexão TCP; cada método localiza a sessão já aberta pelo número de
/// série e envia o comando dentro dela. Application/Equipamentos nunca conhece essa
/// diferença — só <see cref="AppMorador.Application.Jfl.IJflComandoServico"/> (que
/// resolve o Provider por Fabricante) sabe disso.
/// </summary>
public interface IJflProvider
{
    Task<ResultadoTesteConexaoJfl> TestarConexaoAsync(string numeroSerie, CancellationToken cancellationToken);

    Task<ResultadoComandoJfl> ConsultarStatusAsync(string numeroSerie, CancellationToken cancellationToken);

    Task<ResultadoComandoJfl> ArmarAsync(string numeroSerie, int particao, CancellationToken cancellationToken);

    Task<ResultadoComandoJfl> DesarmarAsync(string numeroSerie, int particao, CancellationToken cancellationToken);

    Task<ResultadoComandoJfl> ArmarStayAsync(string numeroSerie, int particao, CancellationToken cancellationToken);

    Task<ResultadoComandoJfl> ArmarAwayAsync(string numeroSerie, int particao, CancellationToken cancellationToken);

    Task<ResultadoComandoJfl> AcionarPgmAsync(string numeroSerie, int pgmNumero, CancellationToken cancellationToken);

    Task<ResultadoComandoJfl> DesligarPgmAsync(string numeroSerie, int pgmNumero, CancellationToken cancellationToken);

    /// <summary>Substitui o conjunto inteiro de zonas inibidas (protocolo não soma — ver ZoneInhibitCommandService). O conjunto final desejado é montado por quem chama.</summary>
    Task<ResultadoComandoJfl> InibirZonasAsync(string numeroSerie, IReadOnlySet<int> zonasQueDevemFicarInibidas, CancellationToken cancellationToken);
}
