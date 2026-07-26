namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 12 — Migração JFL Active 100 Bus (ADR 0015). Rollup persistido do último
/// status conhecido de uma central (1:1 com <see cref="Equipamento"/>, Fabricante=Jfl)
/// — usado só pelo Dashboard, nunca pela tela de detalhes (que sempre consulta a
/// central ao vivo). Atualizado exclusivamente por uma ação explícita do usuário
/// (consultar status, armar, desarmar, PGM, inibir zonas — todo comando da "tela
/// monitorar" devolve o status completo) — nunca por polling automático.
/// </summary>
public class StatusCentralJfl
{
    public Guid Id { get; set; }

    public Guid EquipamentoId { get; set; }

    public Equipamento? Equipamento { get; set; }

    public required DateTime CapturadoEmUtc { get; set; }

    public required int QuantidadeParticoesArmadas { get; set; }

    public required int QuantidadeParticoesDesarmadas { get; set; }

    public required bool TemProblemaAtivo { get; set; }
}
