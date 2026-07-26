using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Autorizacoes;

/// <summary>
/// Ownership resolvido via Autorizacao→MoradorResponsavel→Unidade→Propriedade.ProprietarioId
/// (mesma cadeia já usada em todo o domínio principal). Status efetivo é híbrido (ver ADR
/// 0011): Pendente/Ativa/Expirada são computados a partir de DataInicial/DataFinal/Horario em
/// tempo de leitura — nunca gravados — e StatusManual (Cancelada/Utilizada) sempre vence o
/// cálculo quando presente.
/// </summary>
public sealed class AutorizacaoServico : IAutorizacaoServico
{
    private readonly IVisitanteRepositorio _visitantes;
    private readonly IUnidadeRepositorio _unidades;
    private readonly IMoradorRepositorio _moradores;
    private readonly IAutorizacaoRepositorio _autorizacoes;
    private readonly IHistoricoVisitanteRepositorio _historico;

    public AutorizacaoServico(
        IVisitanteRepositorio visitantes,
        IUnidadeRepositorio unidades,
        IMoradorRepositorio moradores,
        IAutorizacaoRepositorio autorizacoes,
        IHistoricoVisitanteRepositorio historico)
    {
        _visitantes = visitantes;
        _unidades = unidades;
        _moradores = moradores;
        _autorizacoes = autorizacoes;
        _historico = historico;
    }

    public async Task<Result<AutorizacaoResponse>> CreateAsync(
        Guid proprietarioId, Guid visitanteId, CriarAutorizacaoRequest request, CancellationToken cancellationToken)
    {
        var visitante = await _visitantes.GetByIdAsync(visitanteId, cancellationToken).ConfigureAwait(false);
        if (visitante is null || visitante.Propriedade is null || visitante.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<AutorizacaoResponse>.Fail("Visitante não encontrado.");
        }

        var unidade = await _unidades.GetByIdAsync(request.UnidadeId, cancellationToken).ConfigureAwait(false);
        if (unidade is null || unidade.Propriedade is null ||
            unidade.Propriedade.Id != visitante.PropriedadeId || unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            // Mesma mensagem generica de "nao encontrada" — nao revela se a unidade
            // existe em outra propriedade.
            return Result<AutorizacaoResponse>.Fail("Unidade não encontrada.");
        }

        var morador = await _moradores.GetByIdAsync(request.MoradorResponsavelId, cancellationToken).ConfigureAwait(false);
        if (morador is null || morador.UnidadeId != request.UnidadeId)
        {
            return Result<AutorizacaoResponse>.Fail("Morador não encontrado.");
        }

        if (request.DataFinal < request.DataInicial)
        {
            return Result<AutorizacaoResponse>.Fail("Data final não pode ser anterior à data inicial.");
        }

        var autorizacao = new Autorizacao
        {
            Id = Guid.NewGuid(),
            MoradorResponsavelId = request.MoradorResponsavelId,
            UnidadeId = request.UnidadeId,
            VisitanteId = visitanteId,
            Tipo = request.Tipo,
            DataInicial = request.DataInicial,
            DataFinal = request.DataFinal,
            HorarioInicial = request.HorarioInicial,
            HorarioFinal = request.HorarioFinal,
            StatusManual = null,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _autorizacoes.AddAsync(autorizacao, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(
            visitanteId, autorizacao.Id, TipoEventoHistoricoVisitante.AutorizacaoCriada,
            $"Autorização criada para {morador.Nome} ({unidade.Identificacao}).", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _autorizacoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AutorizacaoResponse>.Ok(ToDto(autorizacao, morador.Nome, unidade.Identificacao, visitante.Nome));
    }

    public async Task<Result<IReadOnlyList<AutorizacaoResponse>>> ListByVisitanteAsync(
        Guid proprietarioId, Guid visitanteId, CancellationToken cancellationToken)
    {
        var visitante = await _visitantes.GetByIdAsync(visitanteId, cancellationToken).ConfigureAwait(false);
        if (visitante is null || visitante.Propriedade is null || visitante.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<AutorizacaoResponse>>.Fail("Visitante não encontrado.");
        }

        var autorizacoes = await _autorizacoes.ListByVisitanteAsync(visitanteId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<AutorizacaoResponse>>.Ok(
            autorizacoes.Select(a => ToDto(a, a.MoradorResponsavel?.Nome ?? "", a.Unidade?.Identificacao ?? "", visitante.Nome)).ToList());
    }

    public async Task<Result<AutorizacaoResponse>> UpdateAsync(
        Guid proprietarioId, Guid autorizacaoId, AtualizarAutorizacaoRequest request, CancellationToken cancellationToken)
    {
        var autorizacao = await _autorizacoes.GetByIdAsync(autorizacaoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(autorizacao, proprietarioId))
        {
            return Result<AutorizacaoResponse>.Fail("Autorização não encontrada.");
        }

        if (autorizacao!.StatusManual is StatusAutorizacao.Cancelada or StatusAutorizacao.Utilizada)
        {
            return Result<AutorizacaoResponse>.Fail("Autorização cancelada ou utilizada não pode ser alterada.");
        }

        if (request.DataFinal < request.DataInicial)
        {
            return Result<AutorizacaoResponse>.Fail("Data final não pode ser anterior à data inicial.");
        }

        autorizacao.Tipo = request.Tipo;
        autorizacao.DataInicial = request.DataInicial;
        autorizacao.DataFinal = request.DataFinal;
        autorizacao.HorarioInicial = request.HorarioInicial;
        autorizacao.HorarioFinal = request.HorarioFinal;

        await RegistrarHistoricoAsync(
            autorizacao.VisitanteId, autorizacao.Id, TipoEventoHistoricoVisitante.AutorizacaoAlterada,
            "Regras da autorização alteradas.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _autorizacoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AutorizacaoResponse>.Ok(ToDto(
            autorizacao, autorizacao.MoradorResponsavel?.Nome ?? "", autorizacao.Unidade?.Identificacao ?? "", autorizacao.Visitante?.Nome ?? ""));
    }

    public async Task<Result<AutorizacaoResponse>> AtualizarStatusAsync(
        Guid proprietarioId, Guid autorizacaoId, AtualizarStatusAutorizacaoRequest request, CancellationToken cancellationToken)
    {
        var autorizacao = await _autorizacoes.GetByIdAsync(autorizacaoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(autorizacao, proprietarioId))
        {
            return Result<AutorizacaoResponse>.Fail("Autorização não encontrada.");
        }

        if (request.Status is not (StatusAutorizacao.Cancelada or StatusAutorizacao.Utilizada))
        {
            return Result<AutorizacaoResponse>.Fail("Status inválido — só é possível definir Cancelada ou Utilizada manualmente.");
        }

        if (autorizacao!.StatusManual != request.Status)
        {
            autorizacao.StatusManual = request.Status;
            var tipoEvento = request.Status == StatusAutorizacao.Cancelada
                ? TipoEventoHistoricoVisitante.AutorizacaoCancelada
                : TipoEventoHistoricoVisitante.AutorizacaoUtilizada;

            await RegistrarHistoricoAsync(
                autorizacao.VisitanteId, autorizacao.Id, tipoEvento, $"Autorização marcada como {request.Status}.", proprietarioId, cancellationToken)
                .ConfigureAwait(false);
        }

        await _autorizacoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AutorizacaoResponse>.Ok(ToDto(
            autorizacao, autorizacao.MoradorResponsavel?.Nome ?? "", autorizacao.Unidade?.Identificacao ?? "", autorizacao.Visitante?.Nome ?? ""));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid autorizacaoId, CancellationToken cancellationToken)
    {
        var autorizacao = await _autorizacoes.GetByIdAsync(autorizacaoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(autorizacao, proprietarioId))
        {
            return Result.Fail("Autorização não encontrada.");
        }

        autorizacao!.Excluido = true;
        autorizacao.DataExclusaoUtc = DateTime.UtcNow;
        autorizacao.ExcluidoPorUsuarioId = proprietarioId;

        await RegistrarHistoricoAsync(
            autorizacao.VisitanteId, autorizacao.Id, TipoEventoHistoricoVisitante.AutorizacaoExcluida,
            "Autorização excluída.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _autorizacoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    private async Task RegistrarHistoricoAsync(
        Guid visitanteId, Guid autorizacaoId, TipoEventoHistoricoVisitante tipoEvento, string descricao, Guid usuarioId, CancellationToken cancellationToken)
    {
        await _historico.AddAsync(
            new HistoricoVisitante
            {
                Id = Guid.NewGuid(),
                VisitanteId = visitanteId,
                AutorizacaoId = autorizacaoId,
                TipoEvento = tipoEvento,
                Descricao = descricao,
                UsuarioId = usuarioId,
                CreatedAtUtc = DateTime.UtcNow,
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static bool PertenceAoProprietario(Autorizacao? autorizacao, Guid proprietarioId) =>
        autorizacao?.MoradorResponsavel?.Unidade?.Propriedade is not null &&
        autorizacao.MoradorResponsavel.Unidade.Propriedade.ProprietarioId == proprietarioId;

    private static AutorizacaoResponse ToDto(Autorizacao autorizacao, string moradorNome, string unidadeIdentificacao, string visitanteNome) => new()
    {
        Id = autorizacao.Id,
        MoradorResponsavelId = autorizacao.MoradorResponsavelId,
        MoradorResponsavelNome = moradorNome,
        UnidadeId = autorizacao.UnidadeId,
        UnidadeIdentificacao = unidadeIdentificacao,
        VisitanteId = autorizacao.VisitanteId,
        VisitanteNome = visitanteNome,
        Tipo = autorizacao.Tipo,
        DataInicial = autorizacao.DataInicial,
        DataFinal = autorizacao.DataFinal,
        HorarioInicial = autorizacao.HorarioInicial,
        HorarioFinal = autorizacao.HorarioFinal,
        Status = StatusAutorizacaoCalculator.CalcularEfetivo(autorizacao, DateTime.UtcNow),
    };
}
