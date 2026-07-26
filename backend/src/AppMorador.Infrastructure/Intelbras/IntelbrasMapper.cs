using AppMorador.Application.Intelbras;

namespace AppMorador.Infrastructure.Intelbras;

/// <summary>Única fronteira de tradução entre o wire-format Intelbras e os DTOs internos (mesmo princípio de ControlIdMapper, ADR 0014).</summary>
internal static class IntelbrasMapper
{
    public static StatusCentralIntelbrasInfo ParaStatusInfo(IntelbrasStatusResponse resposta) => new()
    {
        Particoes = resposta.Particoes
            .Select(p => new ParticaoIntelbrasStatusInfo { Numero = p.Numero, Armada = p.Armada })
            .ToList(),
        TemProblemaAtivo = resposta.TemProblemaAtivo,
    };

    public static EventoImportadoIntelbras ParaEventoImportado(IntelbrasEventoWire evento) => new()
    {
        CodigoEventoOriginal = evento.Codigo,
        Descricao = evento.Descricao,
        OcorridoEmUtc = DateTimeOffset.FromUnixTimeSeconds(evento.OcorridoEmUnix).UtcDateTime,
    };
}
