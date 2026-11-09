using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Sprint 24 (ADR 0040) — porta do agregado AlertaCritico; implementação EF Core em Infrastructure.</summary>
public interface IAlertaCriticoRepositorio
{
    Task<AlertaCritico?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Alerta ainda sem confirmação do mesmo tipo na propriedade — evita abrir um alerta novo a cada disparo repetido.</summary>
    Task<AlertaCritico?> GetAbertoAsync(Guid propriedadeId, TipoAlertaCritico tipo, CancellationToken cancellationToken);

    Task<IReadOnlyList<AlertaCritico>> ListByPropriedadeAsync(Guid propriedadeId, bool apenasAbertos, int limite, CancellationToken cancellationToken);

    /// <summary>Alertas sem confirmação cujo próximo escalonamento já venceu.</summary>
    Task<IReadOnlyList<AlertaCritico>> ListEscalonamentoVencidoAsync(DateTime agoraUtc, int limite, CancellationToken cancellationToken);

    /// <summary>
    /// Avança o nível só se o alerta ainda estiver sem confirmação e no <paramref name="nivelEsperado"/>
    /// (atualização condicional no banco). Devolve false quando outra execução já avançou ou alguém
    /// confirmou no meio — quem recebe false não envia nada.
    /// </summary>
    Task<bool> TentarAvancarNivelAsync(
        Guid alertaId,
        NivelEscalonamentoAlerta nivelEsperado,
        NivelEscalonamentoAlerta novoNivel,
        DateTime? proximoEscalonamentoEmUtc,
        DateTime agoraUtc,
        CancellationToken cancellationToken);

    Task AddAsync(AlertaCritico alerta, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
