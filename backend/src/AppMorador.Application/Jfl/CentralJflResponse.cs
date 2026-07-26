using AppMorador.Domain.Entities;

namespace AppMorador.Application.Jfl;

/// <summary>
/// Visão de uma central JFL para a tela "Centrais JFL"/"Detalhes da Central" —
/// combina o cadastro genérico (Equipamento, Sprint 11) com o auto-vínculo por
/// Número de Série à <see cref="AppMorador.Domain.Entities.Central"/> já usada pelo
/// pipeline de eventos (Ocorrencia/Zona, existente desde a Fase 1) e com o último
/// snapshot de status conhecido (<see cref="StatusCentralJfl"/>). Nunca substitui
/// <see cref="EquipamentoResponse"/> genérico — é uma visão especializada só para JFL.
/// </summary>
public sealed class CentralJflResponse
{
    public required Guid EquipamentoId { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public string? Modelo { get; init; }

    public required string NumeroSerie { get; init; }

    public required StatusEquipamento Status { get; init; }

    public DateTime? UltimaSincronizacaoUtc { get; init; }

    /// <summary>Preenchido só quando existe uma Central (pipeline de eventos) com o mesmo Número de Série nesta Propriedade — ver ADR 0015.</summary>
    public Guid? CentralVinculadaId { get; init; }

    public string? CentralVinculadaNome { get; init; }

    public int? QuantidadeParticoesArmadas { get; init; }

    public int? QuantidadeParticoesDesarmadas { get; init; }

    public bool? TemProblemaAtivo { get; init; }
}
