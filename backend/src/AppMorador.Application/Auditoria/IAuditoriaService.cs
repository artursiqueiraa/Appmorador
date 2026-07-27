using AppMorador.Domain.Entities;

namespace AppMorador.Application.Auditoria;

public interface IAuditoriaService
{
    Task RegistrarAsync(
        Guid usuarioId, string usuarioNome, TipoAcaoAuditoria acao, string? entidade, string? entidadeId,
        string? detalhes, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Chamado pelo handler central de autorização (nunca espalhado em Controllers) — ver Program.cs.</summary>
    Task RegistrarFalhaAutorizacaoAsync(Guid? usuarioId, string endpoint, string? ipAddress, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditoriaMaster>> ListarPorUsuarioAsync(Guid usuarioId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditoriaMaster>> ListarPorPropriedadeAsync(Guid propriedadeId, DateTime? inicio, DateTime? fim, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditoriaMaster>> ListarRecentesAsync(int quantidade, CancellationToken cancellationToken);
}
