using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Equipamento — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IEquipamentoRepositorio
{
    Task<Equipamento?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Equipamento>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusEquipamento? status, CancellationToken cancellationToken);

    /// <summary>Sprint 22A (ADR 0029) — total de equipamentos por Status, cross-propriedade, para o Dashboard Operacional.</summary>
    Task<IReadOnlyDictionary<StatusEquipamento, int>> ContarPorStatusGlobalAsync(CancellationToken cancellationToken);

    /// <summary>Usado pelo Dashboard — equipamento com a sincronização mais recente entre todos os da propriedade.</summary>
    Task<DateTime?> GetUltimaSincronizacaoAsync(Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>
    /// Sprint 22B (ADR 0031) — listagem global (cross-propriedade) para o Painel Web. Master/Técnico-only.
    /// <paramref name="incluirRemovidos"/> ignora o filtro global de soft delete (ver
    /// <see cref="EstadoOperacionalEquipamento"/>/soft delete em <c>Equipamento</c>) — uso
    /// administrativo explícito, nunca o comportamento padrão.
    /// </summary>
    Task<(IReadOnlyList<Equipamento> Itens, int Total)> ListarGlobalAsync(
        int pagina, int tamanhoPagina, string? busca, FabricanteEquipamento? fabricante,
        EstadoOperacionalEquipamento? estadoOperacional, bool incluirRemovidos, CancellationToken cancellationToken);

    /// <summary>Sprint 22B (ADR 0031) — checagem amigável antes do índice único no banco (mesma regra, mensagem melhor).</summary>
    Task<bool> ExisteNumeroSerieDuplicadoAsync(
        Guid propriedadeId, string numeroSerie, Guid? excluirEquipamentoId, CancellationToken cancellationToken);

    /// <summary>
    /// Sprint 22C.2 — busca cross-propriedade por Fabricante+Identificador (nunca por Id).
    /// Único ponto usado pelo hook de conexão JFL (<c>EquipamentoJflConexaoObserver</c>, em
    /// Infrastructure) para achar qual Equipamento corresponde a uma central que acabou de
    /// se autenticar pelo Número de Série — a sessão JFL não conhece nenhum Guid, só o
    /// Identificador que a própria central informou no handshake.
    /// </summary>
    Task<Equipamento?> GetByFabricanteEIdentificadorAsync(
        FabricanteEquipamento fabricante, string identificador, CancellationToken cancellationToken);

    Task AddAsync(Equipamento equipamento, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
