using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Credencial — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface ICredencialRepositorio
{
    /// <summary>Inclui Morador, Unidade e Propriedade (navegacao) — quem chama precisa delas para o check de ownership.</summary>
    Task<Credencial?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Credencial>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando uma Unidade inteira e excluida.</summary>
    Task<IReadOnlyList<Credencial>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken);

    /// <summary>Todas as credenciais de todos os moradores de uma propriedade — usado pelo cascade de exclusao e pelo Dashboard.</summary>
    Task<IReadOnlyList<Credencial>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusCredencial? status, CancellationToken cancellationToken);

    Task AddAsync(Credencial credencial, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
