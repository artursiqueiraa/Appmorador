using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class ConsentimentoBiometricoRepositorio : IConsentimentoBiometricoRepositorio
{
    private readonly AppDbContext _db;

    public ConsentimentoBiometricoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public Task<ConsentimentoBiometrico?> GetAtivoAsync(Guid moradorId, CancellationToken cancellationToken) =>
        _db.ConsentimentosBiometricos
            .Where(c => c.MoradorId == moradorId && c.RevogadoEmUtc == null)
            .OrderByDescending(c => c.RegistradoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(ConsentimentoBiometrico consentimento, CancellationToken cancellationToken) =>
        await _db.ConsentimentosBiometricos.AddAsync(consentimento, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}

internal sealed class SincronizacaoFotoRepositorio : ISincronizacaoFotoRepositorio
{
    private readonly AppDbContext _db;

    public SincronizacaoFotoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SincronizacaoFoto>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken) =>
        await _db.SincronizacoesFoto.Include(s => s.Equipamento).Where(s => s.MoradorId == moradorId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<SincronizacaoFoto>> ListFalhasAsync(int limite, CancellationToken cancellationToken) =>
        await _db.SincronizacoesFoto
            .Include(s => s.Equipamento)
            .Where(s => s.Situacao == SituacaoFotoLeitor.Falhou)
            .OrderBy(s => s.AtualizadoEmUtc)
            .Take(limite)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(SincronizacaoFoto sincronizacao, CancellationToken cancellationToken) =>
        await _db.SincronizacoesFoto.AddAsync(sincronizacao, cancellationToken).ConfigureAwait(false);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}
