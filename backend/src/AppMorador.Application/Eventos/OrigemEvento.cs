namespace AppMorador.Application.Eventos;

/// <summary>
/// Qual integração produziu o evento. A resposta de cada evento nunca expõe este
/// valor (protocolo/integração não é linguagem de usuário no corpo da resposta) —
/// mas desde a Sprint 13 (ADR 0016) ele é aceito como filtro de consulta na Central
/// de Eventos, já que o usuário pode legitimamente querer separar "eventos de alarme"
/// de "eventos de controle de acesso". Novas fontes reais (Intelbras, Hikvision, Dahua)
/// ganham um valor aqui só quando existirem de fato.
/// </summary>
public enum OrigemEvento
{
    /// <summary>Central de alarme (protocolo JFL).</summary>
    Jfl,

    /// <summary>Gerado pelo próprio AppMorador, sem integração externa por trás.</summary>
    Aplicacao,

    /// <summary>Sprint 11 — equipamento de controle de acesso integrado via Provider (hoje só Control iD, ver ADR 0014).</summary>
    ControlId,

    /// <summary>Sprint 15 — central de alarme Intelbras integrada via Provider (ADR 0014/0018).</summary>
    Intelbras,
}
