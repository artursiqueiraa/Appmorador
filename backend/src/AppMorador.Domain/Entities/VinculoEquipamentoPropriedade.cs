namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 22B (ADR 0031) — vínculo Equipamento↔Propriedade com histórico. Entidade NOVA,
/// deliberadamente separada de <see cref="Provisionamento"/> (ADR 0028, Sprint 21 — "pacote de
/// instalação" da propriedade, sem noção de equipamento nenhum) para não sobrecarregar um
/// conceito já existente com um sentido diferente — ver ADR 0031.
///
/// "Disponível"/"Provisionado" (estado pedido pela missão) é DERIVADO, não um campo próprio:
/// um Equipamento está "Provisionado" se existe um vínculo com <see cref="DataFimUtc"/> nulo;
/// caso contrário está "Disponível". Um Equipamento nunca tem mais de um vínculo ATIVO
/// (DataFimUtc nulo) simultâneo — regra garantida no Servico (`ProvisionamentoServico`), não no
/// banco: MySQL não suporta índice único parcial/condicional sem colunas geradas, o que seria
/// desproporcional ao escopo desta Sprint (mesma classe de simplificação já registrada em
/// decisões anteriores do projeto, ex.: debounce em memória em vez de Redis).
/// </summary>
public class VinculoEquipamentoPropriedade
{
    public Guid Id { get; set; }

    public Guid EquipamentoId { get; set; }

    public Equipamento? Equipamento { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required DateTime DataInicioUtc { get; set; }

    /// <summary>Nulo enquanto o vínculo está ativo — preenchido ao desvincular/trocar.</summary>
    public DateTime? DataFimUtc { get; set; }

    /// <summary>Usuário interno (Técnico/Master) que criou este vínculo — nunca nulo, sempre auditável.</summary>
    public Guid CriadoPorUsuarioId { get; set; }

    public string? Observacoes { get; set; }
}
