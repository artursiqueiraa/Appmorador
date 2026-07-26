using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Veiculo — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IVeiculoRepositorio
{
    /// <summary>Inclui Morador→Unidade→Propriedade — quem chama precisa delas para o check de ownership.</summary>
    Task<Veiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Busca por placa normalizada (maiúscula, sem espaços) — usado para validar duplicidade. Já respeita o query filter (só considera veículos não excluídos).</summary>
    Task<Veiculo?> GetByPlacaAsync(string placaNormalizada, CancellationToken cancellationToken);

    Task<IReadOnlyList<Veiculo>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando uma Unidade inteira e excluida.</summary>
    Task<IReadOnlyList<Veiculo>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken);

    /// <summary>Todos os veiculos de todos os moradores de uma propriedade — usado pelo cascade de exclusao e pelo Dashboard.</summary>
    Task<IReadOnlyList<Veiculo>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusVeiculo? status, CancellationToken cancellationToken);

    Task AddAsync(Veiculo veiculo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
