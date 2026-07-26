using AppMorador.Domain.Entities;

namespace AppMorador.Application.Operacional;

/// <summary>
/// Estado Bruto de um Equipamento (Sprint 13, ADR 0016) — lido exclusivamente de dados
/// já persistidos pelas integrações existentes (Equipamento.Status, StatusCentralJfl),
/// nunca de uma chamada a um Provider. Entrada do Classificador Operacional.
/// </summary>
public sealed class EstadoBrutoEquipamento
{
    public required Guid EquipamentoId { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public required StatusEquipamento Status { get; init; }

    public DateTime? UltimaComunicacaoUtc { get; init; }

    /// <summary>Só relevante para fabricantes com um rollup de problema conhecido (hoje só JFL, via StatusCentralJfl) — false quando não se aplica.</summary>
    public required bool TemProblemaAtivo { get; init; }
}

/// <summary>Classificação individual de um Equipamento — usada pela tela "Saúde da Propriedade" para explicar o que compõe a saúde consolidada.</summary>
public sealed class EquipamentoSaudeResponse
{
    public required Guid EquipamentoId { get; init; }

    public required string Nome { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public required EstadoOperacional Estado { get; init; }
}

/// <summary>Contrato exposto ao Dashboard/Mobile — nunca contém termos técnicos de fabricante.</summary>
public sealed class SnapshotOperacionalResponse
{
    public required DateTime GeradoEmUtc { get; init; }

    public required EstadoOperacional Saude { get; init; }

    public required int QuantidadeEquipamentosOnline { get; init; }

    public required int QuantidadeEquipamentosOffline { get; init; }

    public DateTime? UltimaComunicacaoUtc { get; init; }

    public required int QuantidadeEventosHoje { get; init; }

    public required int QuantidadeAlarmesAtivos { get; init; }

    public required int QuantidadeFalhasDetectadas { get; init; }

    /// <summary>Sprint 13 — classificação individual de cada equipamento, para a tela "Saúde da Propriedade".</summary>
    public required IReadOnlyList<EquipamentoSaudeResponse> Equipamentos { get; init; }
}
