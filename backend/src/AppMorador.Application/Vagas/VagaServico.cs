using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Vagas;

/// <summary>
/// Ownership resolvido via Vaga→Propriedade.ProprietarioId (mesma cadeia de
/// <see cref="Application.PontosAcesso.PontoAcessoServico"/> — Vaga também pertence
/// direto à Propriedade, nunca ao Morador). Status efetivo é híbrido (ver
/// <see cref="VagaStatusCalculator"/>): Livre/Ocupada computados a partir da
/// existência de um vínculo ativo; Bloqueada/Reservada são overrides manuais.
/// </summary>
public sealed class VagaServico : IVagaServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IVagaRepositorio _vagas;
    private readonly IVinculoVeiculoVagaRepositorio _vinculos;
    private readonly IHistoricoVagaRepositorio _historico;

    public VagaServico(
        IPropriedadeRepositorio propriedades,
        IVagaRepositorio vagas,
        IVinculoVeiculoVagaRepositorio vinculos,
        IHistoricoVagaRepositorio historico)
    {
        _propriedades = propriedades;
        _vagas = vagas;
        _vinculos = vinculos;
        _historico = historico;
    }

    public async Task<Result<VagaResponse>> CreateAsync(
        Guid proprietarioId, Guid propriedadeId, CriarVagaRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<VagaResponse>.Fail("Propriedade não encontrada.");
        }

        var vaga = new Vaga
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Numero = request.Numero.Trim(),
            Bloco = NullIfBlank(request.Bloco),
            Andar = NullIfBlank(request.Andar),
            Coberta = request.Coberta,
            Tipo = request.Tipo,
            StatusManual = null,
            Observacoes = NullIfBlank(request.Observacoes),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _vagas.AddAsync(vaga, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(vaga.Id, TipoEventoHistoricoVaga.VagaCriada, $"Vaga {vaga.Numero} criada.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _vagas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VagaResponse>.Ok(ToDto(vaga, temVinculoAtivo: false));
    }

    public async Task<Result<IReadOnlyList<VagaResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<VagaResponse>>.Fail("Propriedade não encontrada.");
        }

        var vagas = await _vagas.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var vinculosAtivos = await _vinculos.ListAtivosByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var vagasComVinculoAtivo = vinculosAtivos.Select(v => v.VagaId).ToHashSet();

        return Result<IReadOnlyList<VagaResponse>>.Ok(
            vagas.Select(v => ToDto(v, vagasComVinculoAtivo.Contains(v.Id))).ToList());
    }

    public async Task<Result<VagaResponse>> UpdateAsync(
        Guid proprietarioId, Guid vagaId, AtualizarVagaRequest request, CancellationToken cancellationToken)
    {
        var vaga = await _vagas.GetByIdAsync(vagaId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(vaga, proprietarioId))
        {
            return Result<VagaResponse>.Fail("Vaga não encontrada.");
        }

        vaga!.Numero = request.Numero.Trim();
        vaga.Bloco = NullIfBlank(request.Bloco);
        vaga.Andar = NullIfBlank(request.Andar);
        vaga.Coberta = request.Coberta;
        vaga.Tipo = request.Tipo;
        vaga.Observacoes = NullIfBlank(request.Observacoes);

        await RegistrarHistoricoAsync(vaga.Id, TipoEventoHistoricoVaga.VagaAlterada, $"Vaga {vaga.Numero} alterada.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _vagas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var vinculoAtivo = await _vinculos.GetAtivoByVagaAsync(vagaId, cancellationToken).ConfigureAwait(false);
        return Result<VagaResponse>.Ok(ToDto(vaga, vinculoAtivo is not null));
    }

    public async Task<Result<VagaResponse>> AtualizarStatusAsync(
        Guid proprietarioId, Guid vagaId, AtualizarStatusVagaRequest request, CancellationToken cancellationToken)
    {
        var vaga = await _vagas.GetByIdAsync(vagaId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(vaga, proprietarioId))
        {
            return Result<VagaResponse>.Fail("Vaga não encontrada.");
        }

        if (request.Status == StatusVaga.Ocupada)
        {
            return Result<VagaResponse>.Fail("Status inválido — Ocupada é sempre computado a partir do vínculo com um veículo, nunca definido manualmente.");
        }

        // Livre = limpar o override manual (volta a ser computado a partir do vínculo ativo).
        var novoStatusManual = request.Status == StatusVaga.Livre ? (StatusVaga?)null : request.Status;
        if (vaga!.StatusManual != novoStatusManual)
        {
            vaga.StatusManual = novoStatusManual;
            var tipoEvento = request.Status switch
            {
                StatusVaga.Bloqueada => TipoEventoHistoricoVaga.VagaBloqueada,
                StatusVaga.Reservada => TipoEventoHistoricoVaga.VagaReservada,
                _ => TipoEventoHistoricoVaga.VagaLiberada,
            };

            await RegistrarHistoricoAsync(vaga.Id, tipoEvento, $"Vaga {vaga.Numero} marcada como {request.Status}.", proprietarioId, cancellationToken)
                .ConfigureAwait(false);
        }

        await _vagas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var vinculoAtivo = await _vinculos.GetAtivoByVagaAsync(vagaId, cancellationToken).ConfigureAwait(false);
        return Result<VagaResponse>.Ok(ToDto(vaga, vinculoAtivo is not null));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid vagaId, CancellationToken cancellationToken)
    {
        var vaga = await _vagas.GetByIdAsync(vagaId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(vaga, proprietarioId))
        {
            return Result.Fail("Vaga não encontrada.");
        }

        var agora = DateTime.UtcNow;
        // Nenhum Vinculo (ativo ou historico) pode continuar apontando pra uma Vaga excluida.
        var vinculosDaVaga = await _vinculos.ListByVagaAsync(vagaId, cancellationToken).ConfigureAwait(false);

        vaga!.Excluido = true;
        vaga.DataExclusaoUtc = agora;
        vaga.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var vinculo in vinculosDaVaga)
        {
            vinculo.Excluido = true;
            vinculo.DataExclusaoUtc = agora;
            vinculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        await RegistrarHistoricoAsync(vaga.Id, TipoEventoHistoricoVaga.VagaRemovida, $"Vaga {vaga.Numero} excluída.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _vagas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task RegistrarHistoricoAsync(Guid vagaId, TipoEventoHistoricoVaga tipoEvento, string descricao, Guid usuarioId, CancellationToken cancellationToken) =>
        await _historico.AddAsync(
            new HistoricoVaga { Id = Guid.NewGuid(), VagaId = vagaId, TipoEvento = tipoEvento, Descricao = descricao, UsuarioId = usuarioId, CreatedAtUtc = DateTime.UtcNow },
            cancellationToken).ConfigureAwait(false);

    private static bool PertenceAoProprietario(Vaga? vaga, Guid proprietarioId) =>
        vaga?.Propriedade is not null && vaga.Propriedade.ProprietarioId == proprietarioId;

    private static string? NullIfBlank(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static VagaResponse ToDto(Vaga vaga, bool temVinculoAtivo) => new()
    {
        Id = vaga.Id,
        PropriedadeId = vaga.PropriedadeId,
        Numero = vaga.Numero,
        Bloco = vaga.Bloco,
        Andar = vaga.Andar,
        Coberta = vaga.Coberta,
        Tipo = vaga.Tipo,
        Observacoes = vaga.Observacoes,
        Status = VagaStatusCalculator.CalcularEfetivo(vaga, temVinculoAtivo),
    };
}
