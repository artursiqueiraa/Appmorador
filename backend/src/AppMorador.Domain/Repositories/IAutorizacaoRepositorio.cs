using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Autorizacao — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IAutorizacaoRepositorio
{
    /// <summary>Inclui MoradorResponsavel→Unidade→Propriedade, Unidade e Visitante — ownership + validacao de mesma unidade/propriedade.</summary>
    Task<Autorizacao?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Autorizacao>> ListByVisitanteAsync(Guid visitanteId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando uma Unidade inteira e excluida.</summary>
    Task<IReadOnlyList<Autorizacao>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando o Morador responsavel e excluido.</summary>
    Task<IReadOnlyList<Autorizacao>> ListByMoradorResponsavelAsync(Guid moradorId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao da Propriedade e pelos contadores do Dashboard.</summary>
    Task<IReadOnlyList<Autorizacao>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task AddAsync(Autorizacao autorizacao, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
