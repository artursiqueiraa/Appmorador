using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Sprint 27 (ADR 0043) — auditoria de comandos e idempotência.</summary>
public interface IComandoExecutadoRepositorio
{
    /// <summary>Comando do usuário com a mesma chave, desde <paramref name="desdeUtc"/>.</summary>
    Task<ComandoExecutado?> GetPorChaveAsync(Guid usuarioId, string chave, DateTime desdeUtc, CancellationToken cancellationToken);

    /// <summary>
    /// Grava o registro já salvando. Com chave, devolve false se outra requisição do mesmo usuário
    /// reservou a mesma chave antes (índice único) — nada é gravado nesse caso.
    /// </summary>
    Task<bool> TentarReservarAsync(ComandoExecutado comando, CancellationToken cancellationToken);

    Task<IReadOnlyList<ComandoExecutado>> ListarPorPropriedadeAsync(Guid propriedadeId, int limite, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Sprint 27 (ADR 0043) — histórico dos backups agendados.</summary>
public interface IRegistroBackupRepositorio
{
    Task AddAsync(RegistroBackup registro, CancellationToken cancellationToken);

    Task<RegistroBackup?> UltimoAsync(TipoRegistroBackup tipo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
