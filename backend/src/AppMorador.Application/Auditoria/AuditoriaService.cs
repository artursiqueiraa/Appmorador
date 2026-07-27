using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AppMorador.Application.Auditoria;

/// <summary>
/// Sprint 21 (ADR 0021) — nunca lança: uma falha ao registrar auditoria não pode
/// derrubar a ação de negócio que já aconteceu (mesmo racional de "best-effort" já
/// usado para publicação em tempo real, ver ADR 0017) — só loga um warning.
/// </summary>
public sealed class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaMasterRepositorio _auditoria;
    private readonly ILogger<AuditoriaService> _logger;

    public AuditoriaService(IAuditoriaMasterRepositorio auditoria, ILogger<AuditoriaService> logger)
    {
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task RegistrarAsync(
        Guid usuarioId, string usuarioNome, TipoAcaoAuditoria acao, string? entidade, string? entidadeId,
        string? detalhes, string? ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            await _auditoria.AddAsync(new AuditoriaMaster
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                UsuarioNome = usuarioNome,
                Acao = acao,
                Entidade = entidade,
                EntidadeId = entidadeId,
                Detalhes = detalhes,
                IpAddress = ipAddress,
                DataHoraUtc = DateTime.UtcNow,
            }, cancellationToken).ConfigureAwait(false);

            await _auditoria.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao registrar auditoria (usuario {UsuarioId}, acao {Acao})", usuarioId, acao);
        }
    }

    public Task RegistrarFalhaAutorizacaoAsync(Guid? usuarioId, string endpoint, string? ipAddress, CancellationToken cancellationToken) =>
        RegistrarAsync(
            usuarioId ?? Guid.Empty,
            usuarioId is null ? "(anônimo)" : "(não resolvido)",
            TipoAcaoAuditoria.FalhaAutorizacao,
            entidade: "Endpoint",
            entidadeId: endpoint,
            detalhes: null,
            ipAddress,
            cancellationToken);

    public async Task<IReadOnlyList<AuditoriaMaster>> ListarPorUsuarioAsync(
        Guid usuarioId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken) =>
        await _auditoria.ListByUsuarioAsync(usuarioId, inicio, fim, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<AuditoriaMaster>> ListarPorPropriedadeAsync(
        Guid propriedadeId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken) =>
        await _auditoria.ListByPropriedadeAsync(propriedadeId, inicio, fim, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<AuditoriaMaster>> ListarRecentesAsync(int quantidade, CancellationToken cancellationToken) =>
        await _auditoria.ListRecentesAsync(quantidade, cancellationToken).ConfigureAwait(false);
}
