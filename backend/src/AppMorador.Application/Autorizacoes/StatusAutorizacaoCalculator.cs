using AppMorador.Domain.Entities;

namespace AppMorador.Application.Autorizacoes;

/// <summary>
/// Única fonte da regra de status efetivo de uma Autorizacao — usada por
/// <see cref="AutorizacaoServico"/> (mapeamento de DTO) e por
/// <see cref="Application.Dashboard.DashboardServico"/> (contadores), para nunca
/// duplicar a regra de negócio entre os dois.
/// </summary>
public static class StatusAutorizacaoCalculator
{
    /// <summary>
    /// Um override manual (Cancelada/Utilizada) sempre vence; sem override, o status
    /// é computado na hora a partir das datas/horários — nunca exige job/scheduler.
    /// </summary>
    public static StatusAutorizacao CalcularEfetivo(Autorizacao autorizacao, DateTime agoraUtc)
    {
        if (autorizacao.StatusManual is not null)
        {
            return autorizacao.StatusManual.Value;
        }

        var inicio = autorizacao.DataInicial.Date + (autorizacao.HorarioInicial?.ToTimeSpan() ?? TimeSpan.Zero);
        var fim = autorizacao.DataFinal.Date + (autorizacao.HorarioFinal?.ToTimeSpan() ?? new TimeSpan(23, 59, 59));

        if (agoraUtc < inicio)
        {
            return StatusAutorizacao.Pendente;
        }

        return agoraUtc > fim ? StatusAutorizacao.Expirada : StatusAutorizacao.Ativa;
    }
}
