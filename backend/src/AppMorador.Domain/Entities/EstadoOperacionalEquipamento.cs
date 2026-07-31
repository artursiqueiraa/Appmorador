namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 22B — estado ADMINISTRATIVO/de ciclo de vida do equipamento (decidido por um
/// Técnico/Master, nunca inferido automaticamente) — eixo deliberadamente separado de
/// <see cref="StatusEquipamento"/> (conectividade, só muda por teste de conexão/sincronização
/// manual). Um equipamento pode estar "Online" (conectividade) e "EmManutencao" (administrativo)
/// ao mesmo tempo — os dois nunca devem ser fundidos num só campo.
/// </summary>
public enum EstadoOperacionalEquipamento
{
    Ativo,
    EmManutencao,
    Inativo,
    Defeituoso,
}
