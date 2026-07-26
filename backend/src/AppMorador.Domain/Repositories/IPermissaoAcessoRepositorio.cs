using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado PermissaoAcesso — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IPermissaoAcessoRepositorio
{
    /// <summary>Inclui Credencial→Morador→Unidade→Propriedade e PontoAcesso — ownership + validação de mesma propriedade.</summary>
    Task<PermissaoAcesso?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissaoAcesso>> ListByCredencialAsync(Guid credencialId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando um Morador (e suas Credenciais) e excluido.</summary>
    Task<IReadOnlyList<PermissaoAcesso>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando uma Unidade inteira e excluida.</summary>
    Task<IReadOnlyList<PermissaoAcesso>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando uma Propriedade inteira e excluida.</summary>
    Task<IReadOnlyList<PermissaoAcesso>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando um PontoAcesso e excluido — invalida so as permissoes que apontavam pra ele.</summary>
    Task<IReadOnlyList<PermissaoAcesso>> ListByPontoAcessoAsync(Guid pontoAcessoId, CancellationToken cancellationToken);

    Task AddAsync(PermissaoAcesso permissaoAcesso, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
