using AppMorador.Application.Common;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Painel.Equipamentos;

/// <summary>Sprint 22B (ADR 0031) — CRUD global (cross-propriedade) de equipamentos para o Painel Web. Master/Técnico-only.</summary>
public interface IEquipamentoAdminServico
{
    Task<EquipamentosAdminPaginadosResponse> ListarAsync(
        int pagina, int tamanhoPagina, string? busca, FabricanteEquipamento? fabricante,
        EstadoOperacionalEquipamento? estadoOperacional, bool incluirRemovidos, CancellationToken cancellationToken);

    Task<Result<EquipamentoAdminResponse>> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<EquipamentoAdminResponse>> CriarAsync(CriarEquipamentoAdminRequest request, CancellationToken cancellationToken);

    Task<Result<EquipamentoAdminResponse>> AtualizarAsync(Guid id, AtualizarEquipamentoAdminRequest request, CancellationToken cancellationToken);

    Task<Result<EquipamentoAdminResponse>> AtualizarEstadoOperacionalAsync(
        Guid id, EstadoOperacionalEquipamento estadoOperacional, CancellationToken cancellationToken);

    /// <summary>Soft delete (ADR 0009) — nunca remove fisicamente, preserva EventoEquipamento/auditoria.</summary>
    Task<Result> ExcluirAsync(Guid id, CancellationToken cancellationToken);
}
