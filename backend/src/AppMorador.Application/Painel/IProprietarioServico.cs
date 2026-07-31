using AppMorador.Application.Common;

namespace AppMorador.Application.Painel;

/// <summary>Sprint 22A (ADR 0029) — leitura global de clientes (cross-tenant). Master/Suporte-only.</summary>
public interface IProprietarioServico
{
    Task<ProprietariosPaginadosResponse> ListarAsync(int pagina, int tamanhoPagina, string? busca, CancellationToken cancellationToken);

    /// <summary>Detalhe do cliente + suas propriedades — usado pela tela de detalhe (Fase 5) e pela escolha de propriedade ao impersonar (Fase 6).</summary>
    Task<Result<ProprietarioDetalheResponse>> ObterDetalheAsync(Guid id, CancellationToken cancellationToken);
}
