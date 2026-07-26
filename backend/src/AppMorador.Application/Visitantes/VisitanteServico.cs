using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Visitantes;

/// <summary>Ownership resolvido via Visitante→Propriedade.ProprietarioId, mesma cadeia já usada por <see cref="Application.PontosAcesso.PontoAcessoServico"/> (Visitante também pertence direto à Propriedade, ver ADR 0011).</summary>
public sealed class VisitanteServico : IVisitanteServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IVisitanteRepositorio _visitantes;
    private readonly IAutorizacaoRepositorio _autorizacoes;
    private readonly IHistoricoVisitanteRepositorio _historico;

    public VisitanteServico(
        IPropriedadeRepositorio propriedades,
        IVisitanteRepositorio visitantes,
        IAutorizacaoRepositorio autorizacoes,
        IHistoricoVisitanteRepositorio historico)
    {
        _propriedades = propriedades;
        _visitantes = visitantes;
        _autorizacoes = autorizacoes;
        _historico = historico;
    }

    public async Task<Result<VisitanteResponse>> CreateAsync(
        Guid proprietarioId, Guid propriedadeId, CriarVisitanteRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<VisitanteResponse>.Fail("Propriedade não encontrada.");
        }

        var visitante = new Visitante
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Nome = request.Nome.Trim(),
            Documento = NullIfBlank(request.Documento),
            Telefone = NullIfBlank(request.Telefone),
            Observacoes = NullIfBlank(request.Observacoes),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _visitantes.AddAsync(visitante, cancellationToken).ConfigureAwait(false);
        await _visitantes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VisitanteResponse>.Ok(ToDto(visitante));
    }

    public async Task<Result<IReadOnlyList<VisitanteResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<VisitanteResponse>>.Fail("Propriedade não encontrada.");
        }

        var visitantes = await _visitantes.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<VisitanteResponse>>.Ok(visitantes.Select(ToDto).ToList());
    }

    public async Task<Result<VisitanteResponse>> UpdateAsync(
        Guid proprietarioId, Guid visitanteId, AtualizarVisitanteRequest request, CancellationToken cancellationToken)
    {
        var visitante = await _visitantes.GetByIdAsync(visitanteId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(visitante?.Propriedade, proprietarioId))
        {
            return Result<VisitanteResponse>.Fail("Visitante não encontrado.");
        }

        visitante!.Nome = request.Nome.Trim();
        visitante.Documento = NullIfBlank(request.Documento);
        visitante.Telefone = NullIfBlank(request.Telefone);
        visitante.Observacoes = NullIfBlank(request.Observacoes);
        await _visitantes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VisitanteResponse>.Ok(ToDto(visitante));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid visitanteId, CancellationToken cancellationToken)
    {
        var visitante = await _visitantes.GetByIdAsync(visitanteId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(visitante?.Propriedade, proprietarioId))
        {
            return Result.Fail("Visitante não encontrado.");
        }

        // Nenhuma Autorizacao pode continuar apontando pra um Visitante excluido.
        var agora = DateTime.UtcNow;
        var autorizacoesDoVisitante = await _autorizacoes.ListByVisitanteAsync(visitanteId, cancellationToken).ConfigureAwait(false);

        visitante!.Excluido = true;
        visitante.DataExclusaoUtc = agora;
        visitante.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var autorizacao in autorizacoesDoVisitante)
        {
            autorizacao.Excluido = true;
            autorizacao.DataExclusaoUtc = agora;
            autorizacao.ExcluidoPorUsuarioId = proprietarioId;
        }

        await _historico.AddAsync(
            new HistoricoVisitante
            {
                Id = Guid.NewGuid(),
                VisitanteId = visitante.Id,
                AutorizacaoId = null,
                TipoEvento = TipoEventoHistoricoVisitante.VisitanteRemovido,
                Descricao = $"Visitante \"{visitante.Nome}\" removido.",
                UsuarioId = proprietarioId,
                CreatedAtUtc = agora,
            },
            cancellationToken).ConfigureAwait(false);

        await _visitantes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private static bool PertenceAoProprietario(Propriedade? propriedade, Guid proprietarioId) =>
        propriedade is not null && propriedade.ProprietarioId == proprietarioId;

    private static string? NullIfBlank(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static VisitanteResponse ToDto(Visitante visitante) => new()
    {
        Id = visitante.Id,
        PropriedadeId = visitante.PropriedadeId,
        Nome = visitante.Nome,
        Documento = visitante.Documento,
        Telefone = visitante.Telefone,
        FotoPath = visitante.FotoPath,
        Observacoes = visitante.Observacoes,
    };
}
