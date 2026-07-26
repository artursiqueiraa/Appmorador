using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado VinculoVeiculoVaga — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IVinculoVeiculoVagaRepositorio
{
    /// <summary>O vinculo ativo (DataFimUtc nulo) de um Veiculo, se existir. Inclui Vaga (navegacao).</summary>
    Task<VinculoVeiculoVaga?> GetAtivoByVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken);

    /// <summary>O vinculo ativo (DataFimUtc nulo) de uma Vaga, se existir — usado para checar ocupacao antes de vincular.</summary>
    Task<VinculoVeiculoVaga?> GetAtivoByVagaAsync(Guid vagaId, CancellationToken cancellationToken);

    /// <summary>Historico completo (ativos e encerrados) de um Veiculo — exibicao e cascade de exclusao.</summary>
    Task<IReadOnlyList<VinculoVeiculoVaga>> ListByVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken);

    /// <summary>Historico completo de uma Vaga — usado pelo cascade de exclusao quando a Vaga e excluida.</summary>
    Task<IReadOnlyList<VinculoVeiculoVaga>> ListByVagaAsync(Guid vagaId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando Unidade/Morador sao excluidos (varios veiculos de uma vez).</summary>
    Task<IReadOnlyList<VinculoVeiculoVaga>> ListByVeiculosAsync(IReadOnlyList<Guid> veiculoIds, CancellationToken cancellationToken);

    /// <summary>Vinculos ativos de todas as Vagas de uma propriedade — usado para computar o Status efetivo das Vagas e os contadores do Dashboard.</summary>
    Task<IReadOnlyList<VinculoVeiculoVaga>> ListAtivosByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Historico completo de todas as Vagas de uma propriedade — usado pelo cascade de exclusao da Propriedade.</summary>
    Task<IReadOnlyList<VinculoVeiculoVaga>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(VinculoVeiculoVaga vinculo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
