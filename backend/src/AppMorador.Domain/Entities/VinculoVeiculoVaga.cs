using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Vínculo Veículo ↔ Vaga como entidade própria (nunca armazenado dentro de Veiculo) —
/// tabela histórica por natureza: cada linha é um período de ocupação. Ativo quando
/// <see cref="DataFimUtc"/> é nulo (no máximo 1 vínculo ativo por Veículo e por Vaga ao
/// mesmo tempo, validado em <c>VeiculoVagaServico</c>). Prepara vagas rotativas futuras
/// sem precisar de redesenho.
/// </summary>
public class VinculoVeiculoVaga : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid VeiculoId { get; set; }

    public Veiculo? Veiculo { get; set; }

    public Guid VagaId { get; set; }

    public Vaga? Vaga { get; set; }

    public required DateTime DataInicioUtc { get; set; }

    /// <summary>Nulo = vínculo ativo (o veículo está na vaga agora).</summary>
    public DateTime? DataFimUtc { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
