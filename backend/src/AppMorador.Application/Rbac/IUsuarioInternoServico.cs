using AppMorador.Application.Common;

namespace AppMorador.Application.Rbac;

/// <summary>Sprint 21 (ADR 0021) — CRUD (parcial: sem edição, só criar/listar/desativar) das contas internas da plataforma (Master/Tecnico/Suporte). Exclusivo de Master.</summary>
public interface IUsuarioInternoServico
{
    Task<Result<UsuarioInternoResponse>> CriarAsync(CriarUsuarioInternoRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<UsuarioInternoResponse>> ListarAsync(CancellationToken cancellationToken);

    Task<Result> DesativarAsync(Guid id, CancellationToken cancellationToken);
}
