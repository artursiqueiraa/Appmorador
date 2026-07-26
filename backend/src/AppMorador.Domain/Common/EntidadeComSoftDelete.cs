namespace AppMorador.Domain.Common;

/// <summary>
/// Base para entidades do dominio principal que usam exclusao logica (Sprint 6, ADR
/// 0009): excluir nunca remove a linha do banco — so marca <see cref="Excluido"/> e
/// registra quando/por quem, para permitir auditoria e uma futura restauracao
/// (Lixeira). Toda consulta normal via <see cref="Infrastructure.Persistence.AppDbContext"/>
/// ja filtra <c>Excluido == false</c> automaticamente (query filter global) — quem
/// implementa um repositorio novo sobre uma entidade que herda daqui nao precisa
/// lembrar de excluir manualmente os registros apagados de cada consulta.
/// </summary>
public abstract class EntidadeComSoftDelete
{
    public bool Excluido { get; set; }

    public DateTime? DataExclusaoUtc { get; set; }

    /// <summary>Usuario que executou a exclusao — nulo enquanto o registro nao foi excluido.</summary>
    public Guid? ExcluidoPorUsuarioId { get; set; }
}
