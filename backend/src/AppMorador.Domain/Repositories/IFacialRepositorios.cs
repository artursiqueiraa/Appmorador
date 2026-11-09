using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Sprint 26 (ADR 0042) — consentimentos biométricos.</summary>
public interface IConsentimentoBiometricoRepositorio
{
    /// <summary>Consentimento não revogado da pessoa, se houver.</summary>
    Task<ConsentimentoBiometrico?> GetAtivoAsync(Guid moradorId, CancellationToken cancellationToken);

    Task AddAsync(ConsentimentoBiometrico consentimento, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Sprint 26 (ADR 0042) — situação da foto em cada leitor.</summary>
public interface ISincronizacaoFotoRepositorio
{
    Task<IReadOnlyList<SincronizacaoFoto>> ListByMoradorAsync(Guid moradorId, CancellationToken cancellationToken);

    /// <summary>Registros com Situacao = Falhou (para nova tentativa).</summary>
    Task<IReadOnlyList<SincronizacaoFoto>> ListFalhasAsync(int limite, CancellationToken cancellationToken);

    Task AddAsync(SincronizacaoFoto sincronizacao, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
