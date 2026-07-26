using AppMorador.Domain.Entities;

namespace AppMorador.Application.Intelbras;

/// <summary>
/// Visão de uma central Intelbras para a tela "Centrais Intelbras"/"Detalhes" —
/// combina o cadastro genérico (Equipamento) com o último status conhecido. Nunca
/// substitui <see cref="AppMorador.Application.Equipamentos.EquipamentoResponse"/>
/// genérico — é uma visão especializada, mesmo espírito de CentralJflResponse.
/// </summary>
public sealed class CentralIntelbrasResponse
{
    public required Guid EquipamentoId { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public string? Modelo { get; init; }

    public required StatusEquipamento Status { get; init; }

    public DateTime? UltimaSincronizacaoUtc { get; init; }

    public int? QuantidadeParticoesArmadas { get; init; }

    public int? QuantidadeParticoesDesarmadas { get; init; }

    public bool? TemProblemaAtivo { get; init; }
}
