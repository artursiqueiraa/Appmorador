using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class AlertaCriticoRepositorio : IAlertaCriticoRepositorio
{
    private readonly AppDbContext _db;

    public AlertaCriticoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<AlertaCritico?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.AlertasCriticos
            .Include(a => a.ConfirmadoPorUsuario)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<AlertaCritico?> GetAbertoAsync(Guid propriedadeId, TipoAlertaCritico tipo, CancellationToken cancellationToken) =>
        _db.AlertasCriticos
            .Where(a => a.PropriedadeId == propriedadeId && a.Tipo == tipo && a.ConfirmadoEmUtc == null)
            .OrderByDescending(a => a.CriadoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AlertaCritico>> ListByPropriedadeAsync(
        Guid propriedadeId, bool apenasAbertos, int limite, CancellationToken cancellationToken)
    {
        var query = _db.AlertasCriticos.Include(a => a.ConfirmadoPorUsuario).Where(a => a.PropriedadeId == propriedadeId);
        if (apenasAbertos)
        {
            query = query.Where(a => a.ConfirmadoEmUtc == null);
        }

        return await query.OrderByDescending(a => a.CriadoEmUtc).Take(limite).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AlertaCritico>> ListEscalonamentoVencidoAsync(DateTime agoraUtc, int limite, CancellationToken cancellationToken) =>
        await _db.AlertasCriticos
            .AsNoTracking()
            .Where(a => a.ConfirmadoEmUtc == null && a.ProximoEscalonamentoEmUtc != null && a.ProximoEscalonamentoEmUtc <= agoraUtc)
            .OrderBy(a => a.ProximoEscalonamentoEmUtc)
            .Take(limite)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<bool> TentarAvancarNivelAsync(
        Guid alertaId,
        NivelEscalonamentoAlerta nivelEsperado,
        NivelEscalonamentoAlerta novoNivel,
        DateTime? proximoEscalonamentoEmUtc,
        DateTime agoraUtc,
        CancellationToken cancellationToken)
    {
        // UPDATE ... WHERE Nivel = esperado AND não confirmado: duas execuções concorrentes (ou uma
        // confirmação no meio) nunca avançam o mesmo nível duas vezes.
        var query = _db.AlertasCriticos.Where(a =>
            a.Id == alertaId && a.NivelEscalonamento == nivelEsperado && a.ConfirmadoEmUtc == null && a.ProximoEscalonamentoEmUtc != null);

        var linhas = novoNivel switch
        {
            NivelEscalonamentoAlerta.Membros when novoNivel != nivelEsperado => await query.ExecuteUpdateAsync(
                set => set
                    .SetProperty(a => a.NivelEscalonamento, novoNivel)
                    .SetProperty(a => a.ProximoEscalonamentoEmUtc, proximoEscalonamentoEmUtc)
                    .SetProperty(a => a.EscalonadoMembrosEmUtc, agoraUtc),
                cancellationToken).ConfigureAwait(false),

            NivelEscalonamentoAlerta.ContatosEmergencia when novoNivel != nivelEsperado => await query.ExecuteUpdateAsync(
                set => set
                    .SetProperty(a => a.NivelEscalonamento, novoNivel)
                    .SetProperty(a => a.ProximoEscalonamentoEmUtc, proximoEscalonamentoEmUtc)
                    .SetProperty(a => a.EscalonadoContatosEmUtc, agoraUtc),
                cancellationToken).ConfigureAwait(false),

            // Mesmo nível: só encerra (ou reagenda) o escalonamento.
            _ => await query.ExecuteUpdateAsync(
                set => set.SetProperty(a => a.ProximoEscalonamentoEmUtc, proximoEscalonamentoEmUtc),
                cancellationToken).ConfigureAwait(false),
        };

        return linhas == 1;
    }

    public async Task AddAsync(AlertaCritico alerta, CancellationToken cancellationToken) =>
        await _db.AlertasCriticos.AddAsync(alerta, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
