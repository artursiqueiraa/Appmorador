using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

/// <summary>Sprint 27 (ADR 0043) — auditoria de comandos; a chave de idempotência é única por usuário.</summary>
internal sealed class ComandoExecutadoRepositorio : IComandoExecutadoRepositorio
{
    private readonly AppDbContext _db;

    public ComandoExecutadoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<ComandoExecutado?> GetPorChaveAsync(Guid usuarioId, string chave, DateTime desdeUtc, CancellationToken cancellationToken) =>
        _db.ComandosExecutados.AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.ChaveIdempotencia == chave && c.OcorridoEmUtc >= desdeUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> TentarReservarAsync(ComandoExecutado comando, CancellationToken cancellationToken)
    {
        var entrada = await _db.ComandosExecutados.AddAsync(comando, cancellationToken).ConfigureAwait(false);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException) when (comando.ChaveIdempotencia is not null)
        {
            // Índice único (UsuarioId, ChaveIdempotencia): outra requisição reservou primeiro.
            entrada.State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<ComandoExecutado>> ListarPorPropriedadeAsync(Guid propriedadeId, int limite, CancellationToken cancellationToken) =>
        await _db.ComandosExecutados.AsNoTracking()
            .Where(c => c.PropriedadeId == propriedadeId && c.Concluido)
            .OrderByDescending(c => c.OcorridoEmUtc)
            .Take(limite)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}

/// <summary>Sprint 27 (ADR 0043) — histórico dos backups agendados.</summary>
internal sealed class RegistroBackupRepositorio : IRegistroBackupRepositorio
{
    private readonly AppDbContext _db;

    public RegistroBackupRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RegistroBackup registro, CancellationToken cancellationToken) =>
        await _db.RegistrosBackup.AddAsync(registro, cancellationToken).ConfigureAwait(false);

    public Task<RegistroBackup?> UltimoAsync(TipoRegistroBackup tipo, CancellationToken cancellationToken) =>
        _db.RegistrosBackup.AsNoTracking()
            .Where(r => r.Tipo == tipo)
            .OrderByDescending(r => r.OcorridoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
