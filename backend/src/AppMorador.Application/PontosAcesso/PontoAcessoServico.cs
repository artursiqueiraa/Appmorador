using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.PontosAcesso;

/// <summary>Mesmo padrão de ownership/soft delete de <see cref="Application.Unidades.UnidadeServico"/>.</summary>
public sealed class PontoAcessoServico : IPontoAcessoServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IPontoAcessoRepositorio _pontosAcesso;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IPermissaoVeicularRepositorio _permissoesVeiculares;

    public PontoAcessoServico(
        IPropriedadeRepositorio propriedades,
        IPontoAcessoRepositorio pontosAcesso,
        IPermissaoAcessoRepositorio permissoes,
        IPermissaoVeicularRepositorio permissoesVeiculares)
    {
        _propriedades = propriedades;
        _pontosAcesso = pontosAcesso;
        _permissoes = permissoes;
        _permissoesVeiculares = permissoesVeiculares;
    }

    public async Task<Result<PontoAcessoResponse>> CreateAsync(
        Guid proprietarioId, Guid propriedadeId, CriarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<PontoAcessoResponse>.Fail("Propriedade não encontrada.");
        }

        var pontoAcesso = new PontoAcesso
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Nome = request.Nome.Trim(),
            Tipo = request.Tipo ?? TipoPontoAcesso.Geral,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _pontosAcesso.AddAsync(pontoAcesso, cancellationToken).ConfigureAwait(false);
        await _pontosAcesso.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PontoAcessoResponse>.Ok(ToDto(pontoAcesso));
    }

    public async Task<Result<IReadOnlyList<PontoAcessoResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<PontoAcessoResponse>>.Fail("Propriedade não encontrada.");
        }

        var pontos = await _pontosAcesso.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<PontoAcessoResponse>>.Ok(pontos.Select(ToDto).ToList());
    }

    public async Task<Result<PontoAcessoResponse>> UpdateAsync(
        Guid proprietarioId, Guid pontoAcessoId, AtualizarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var pontoAcesso = await _pontosAcesso.GetByIdAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);
        if (pontoAcesso is null || pontoAcesso.Propriedade is null || pontoAcesso.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<PontoAcessoResponse>.Fail("Ponto de acesso não encontrado.");
        }

        pontoAcesso.Nome = request.Nome.Trim();
        pontoAcesso.Tipo = request.Tipo ?? pontoAcesso.Tipo;
        await _pontosAcesso.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PontoAcessoResponse>.Ok(ToDto(pontoAcesso));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid pontoAcessoId, CancellationToken cancellationToken)
    {
        var pontoAcesso = await _pontosAcesso.GetByIdAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);
        if (pontoAcesso is null || pontoAcesso.Propriedade is null || pontoAcesso.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result.Fail("Ponto de acesso não encontrado.");
        }

        var agora = DateTime.UtcNow;
        // Nenhuma Permissao (de acesso ou veicular, Sprint 9) pode continuar apontando
        // pra um Ponto de Acesso excluido.
        var permissoesDoPonto = await _permissoes.ListByPontoAcessoAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);
        var permissoesVeicularesDoPonto = await _permissoesVeiculares.ListByPontoAcessoAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);

        pontoAcesso.Excluido = true;
        pontoAcesso.DataExclusaoUtc = agora;
        pontoAcesso.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var permissao in permissoesDoPonto)
        {
            permissao.Excluido = true;
            permissao.DataExclusaoUtc = agora;
            permissao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissaoVeicular in permissoesVeicularesDoPonto)
        {
            permissaoVeicular.Excluido = true;
            permissaoVeicular.DataExclusaoUtc = agora;
            permissaoVeicular.ExcluidoPorUsuarioId = proprietarioId;
        }

        await _pontosAcesso.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private static PontoAcessoResponse ToDto(PontoAcesso pontoAcesso) => new()
    {
        Id = pontoAcesso.Id,
        PropriedadeId = pontoAcesso.PropriedadeId,
        Nome = pontoAcesso.Nome,
        Tipo = pontoAcesso.Tipo,
    };
}
