namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 13 — Camada Operacional Unificada (ADR 0016). Rollup 1:1 por Propriedade,
/// gerado a partir de dados já persistidos pelas integrações existentes (Equipamento,
/// StatusCentralJfl, Central de Eventos) — nunca consulta um Provider diretamente.
/// Atualizado por uma ação explícita (leitura que gera sob demanda, ou o botão
/// "Atualizar" no mobile) — nunca por job/polling automático.
/// </summary>
public class SnapshotOperacional
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required DateTime GeradoEmUtc { get; set; }

    public required EstadoOperacional Saude { get; set; }

    public required int QuantidadeEquipamentosOnline { get; set; }

    public required int QuantidadeEquipamentosOffline { get; set; }

    public DateTime? UltimaComunicacaoUtc { get; set; }

    public required int QuantidadeEventosHoje { get; set; }

    /// <summary>Centrais/equipamentos com uma condição de alarme/problema ativa no último status conhecido (ex.: StatusCentralJfl.TemProblemaAtivo).</summary>
    public required int QuantidadeAlarmesAtivos { get; set; }

    /// <summary>Equipamentos com Status = Offline (tentativa de comunicação que falhou explicitamente — diferente de Desconhecido, nunca testado).</summary>
    public required int QuantidadeFalhasDetectadas { get; set; }
}
