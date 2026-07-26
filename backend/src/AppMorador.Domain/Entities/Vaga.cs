using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Pertence direto à Propriedade (nunca ao Morador) — domínio independente de Veículo.
/// Status efetivo é híbrido (ver <see cref="AppMorador.Application.Vagas.VagaStatusCalculator"/>,
/// mesmo espírito do ADR 0011): Livre/Ocupada são computados a partir da existência de um
/// <see cref="VinculoVeiculoVaga"/> ativo; <see cref="StatusManual"/> (Bloqueada/Reservada)
/// sempre vence o cálculo quando presente.
/// </summary>
public class Vaga : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required string Numero { get; set; }

    public string? Bloco { get; set; }

    public string? Andar { get; set; }

    public required bool Coberta { get; set; }

    public required TipoVaga Tipo { get; set; }

    /// <summary>Nulo = sem override manual (status efetivo computado por vínculo ativo). Só assume Bloqueada ou Reservada.</summary>
    public StatusVaga? StatusManual { get; set; }

    public string? Observacoes { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
