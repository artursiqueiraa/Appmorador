using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o vínculo Equipamento↔Propriedade (Sprint 22B, ADR 0031).</summary>
public interface IVinculoEquipamentoPropriedadeRepositorio
{
    Task<VinculoEquipamentoPropriedade?> GetVinculoAtivoPorEquipamentoAsync(Guid equipamentoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VinculoEquipamentoPropriedade>> ListarHistoricoPorEquipamentoAsync(Guid equipamentoId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<VinculoEquipamentoPropriedade> Itens, int Total)> ListarAtivosGlobalAsync(
        int pagina, int tamanhoPagina, CancellationToken cancellationToken);

    /// <summary>Sprint 22B — total de equipamentos com vínculo ativo ("Provisionado") vs. sem ("Disponível"), para o dashboard de alocação.</summary>
    Task<int> ContarEquipamentosProvisionadosAsync(CancellationToken cancellationToken);

    Task AddAsync(VinculoEquipamentoPropriedade vinculo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
