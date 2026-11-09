using AppMorador.Application.Common;

namespace AppMorador.Application.PontosAcesso;

public interface IPontoAcessoServico
{
    Task<Result<PontoAcessoResponse>> CreateAsync(Guid proprietarioId, Guid propriedadeId, CriarPontoAcessoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PontoAcessoResponse>>> ListByPropriedadeAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<PontoAcessoResponse>> UpdateAsync(Guid proprietarioId, Guid pontoAcessoId, AtualizarPontoAcessoRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(Guid proprietarioId, Guid pontoAcessoId, CancellationToken cancellationToken);

    /// <summary>Sprint 24.5 — equipamentos da propriedade que sabem abrir porta (JFL e Control iD).</summary>
    Task<Result<IReadOnlyList<AcionadorDisponivelResponse>>> ListarAcionadoresAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Sprint 24.5 — abre a porta pelo acionador vinculado e registra quem abriu.</summary>
    Task<Result<ResultadoAberturaResponse>> AbrirAsync(Guid usuarioId, Guid pontoAcessoId, CancellationToken cancellationToken);
}
