using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado PermissaoVeicular — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IPermissaoVeicularRepositorio
{
    /// <summary>Inclui Veiculo→Morador→Unidade→Propriedade e PontoAcesso — ownership + validação de mesma propriedade/tipo.</summary>
    Task<PermissaoVeicular?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissaoVeicular>> ListByVeiculoAsync(Guid veiculoId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando Unidade/Morador sao excluidos (varios veiculos de uma vez).</summary>
    Task<IReadOnlyList<PermissaoVeicular>> ListByVeiculosAsync(IReadOnlyList<Guid> veiculoIds, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao da Propriedade.</summary>
    Task<IReadOnlyList<PermissaoVeicular>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando um PontoAcesso e excluido.</summary>
    Task<IReadOnlyList<PermissaoVeicular>> ListByPontoAcessoAsync(Guid pontoAcessoId, CancellationToken cancellationToken);

    Task AddAsync(PermissaoVeicular permissaoVeicular, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
