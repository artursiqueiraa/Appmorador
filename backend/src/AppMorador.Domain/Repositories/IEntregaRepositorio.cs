using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado Entrega — a implementacao (EF Core) vive em Infrastructure.</summary>
public interface IEntregaRepositorio
{
    /// <summary>Inclui MoradorDestinatario→Unidade→Propriedade e Unidade — ownership + validação de mesma unidade.</summary>
    Task<Entrega?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando o Morador destinatario e excluido.</summary>
    Task<IReadOnlyList<Entrega>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken);

    /// <summary>Usado pelo cascade de exclusao quando uma Unidade inteira e excluida.</summary>
    Task<IReadOnlyList<Entrega>> ListByUnidadeAsync(Guid unidadeId, CancellationToken cancellationToken);

    /// <summary>Lista principal de exibicao (visao unificada da propriedade) e usada pelo cascade de exclusao da Propriedade.</summary>
    Task<IReadOnlyList<Entrega>> ListByPropriedadeAsync(Guid propriedadeId, CancellationToken cancellationToken);

    Task<int> CountByPropriedadeAsync(Guid propriedadeId, StatusEntrega? status, CancellationToken cancellationToken);

    Task AddAsync(Entrega entrega, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
