using AppMorador.Application.ControlId;

namespace AppMorador.Infrastructure.ControlId;

/// <summary>
/// Única fronteira de tradução entre DTOs internos (Application/ControlId) e DTOs de
/// wire-format Control iD (Infrastructure/ControlId) — nenhum outro ponto do sistema
/// pode montar/ler um payload Control iD diretamente (ver ADR 0014).
/// </summary>
internal static class ControlIdMapper
{
    public static ControlIdCreateObjectsRequest ParaObjetoUsuario(MoradorParaSincronizar morador) => new()
    {
        Object = "users",
        Values = new Dictionary<string, object?>
        {
            ["name"] = morador.Nome,
            ["registration"] = morador.MoradorId.ToString(),
        },
    };

    public static ControlIdCreateObjectsRequest ParaObjetoCredencial(CredencialParaSincronizar credencial) => new()
    {
        Object = "cards",
        Values = new Dictionary<string, object?>
        {
            ["user_id"] = credencial.MoradorId.ToString(),
            ["value"] = credencial.Valor,
            ["type"] = credencial.TipoCredencial,
        },
    };

    public static ControlIdCreateObjectsRequest ParaObjetoPermissao(PermissaoParaSincronizar permissao) => new()
    {
        Object = "access_rules",
        Values = new Dictionary<string, object?>
        {
            ["card_id"] = permissao.CredencialId.ToString(),
            ["week_days"] = permissao.DiasPermitidos,
            ["start_time"] = permissao.HorarioInicial?.ToString("HH:mm"),
            ["end_time"] = permissao.HorarioFinal?.ToString("HH:mm"),
        },
    };

    public static InformacoesEquipamento ParaInformacoesEquipamento(ControlIdInformationResponse resposta) => new()
    {
        Versao = resposta.Version ?? "desconhecida",
        NomeDispositivo = resposta.Name,
        NumeroSerie = resposta.DeviceId,
    };

    public static EventoImportado ParaEventoImportado(ControlIdAccessLogEntry entrada) => new()
    {
        CodigoEventoOriginal = entrada.Event.ToString(),
        Descricao = DescreverEvento(entrada.Event),
        OcorridoEmUtc = DateTimeOffset.FromUnixTimeSeconds(entrada.Time).UtcDateTime,
    };

    // Mapeamento best-effort: o legado nunca documentou os códigos reais de access_logs
    // (nunca validado contra hardware). Fallback genérico evita mostrar o código cru.
    private static string DescreverEvento(int codigo) => codigo switch
    {
        0 => "Acesso liberado",
        1 => "Acesso negado",
        _ => "Evento de acesso registrado",
    };
}
