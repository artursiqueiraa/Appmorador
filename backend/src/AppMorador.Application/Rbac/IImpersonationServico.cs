using AppMorador.Application.Common;

namespace AppMorador.Application.Rbac;

/// <summary>
/// Sprint 21 (ADR 0021, Fase 3) — "entrar como cliente". Exclusivo de Master/Suporte
/// (Técnico NÃO tem essa capacidade, ver tabela de papéis da missão). Token de
/// impersonation nunca gera refresh token — a sessão de impersonation termina
/// sozinha em 15 minutos, sem exceção.
/// </summary>
public interface IImpersonationServico
{
    /// <summary>masterNome é resolvido internamente via IUsuarioRepositorio (nunca confiado a partir de uma claim do JWT do chamador, que não carrega nome de exibição).</summary>
    Task<Result<ImpersonarResponse>> IniciarAsync(
        Guid masterId, Guid propriedadeId, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>"Sair da visualização" explícito — o token já expiraria sozinho em 15min, mas isto registra o fim real da sessão de suporte na auditoria.</summary>
    Task EncerrarAsync(Guid masterId, Guid propriedadeId, string? ipAddress, CancellationToken cancellationToken);
}
