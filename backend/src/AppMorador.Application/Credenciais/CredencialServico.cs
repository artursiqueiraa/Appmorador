using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Credenciais;

/// <summary>
/// Ownership resolvido subindo a cadeia Credencial → Morador → Unidade →
/// Propriedade.ProprietarioId — mesmo padrão já usado desde a Sprint 6. Toda
/// alteração relevante (criação, mudança de status, exclusão) grava uma linha em
/// <see cref="HistoricoCredencial"/> (Sprint 7 — auditoria interna, sem leitura
/// ainda, ver ADR 0010).
/// </summary>
public sealed class CredencialServico : ICredencialServico
{
    private readonly IMoradorRepositorio _moradores;
    private readonly ICredencialRepositorio _credenciais;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IHistoricoCredencialRepositorio _historico;

    public CredencialServico(
        IMoradorRepositorio moradores,
        ICredencialRepositorio credenciais,
        IPermissaoAcessoRepositorio permissoes,
        IHistoricoCredencialRepositorio historico)
    {
        _moradores = moradores;
        _credenciais = credenciais;
        _permissoes = permissoes;
        _historico = historico;
    }

    public async Task<Result<CredencialResponse>> CreateAsync(
        Guid proprietarioId, Guid moradorId, CriarCredencialRequest request, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(morador?.Unidade?.Propriedade, proprietarioId))
        {
            return Result<CredencialResponse>.Fail("Morador não encontrado.");
        }

        var credencial = new Credencial
        {
            Id = Guid.NewGuid(),
            MoradorId = moradorId,
            Tipo = request.Tipo,
            Status = StatusCredencial.Ativa,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _credenciais.AddAsync(credencial, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(
            credencial.Id, TipoEventoHistorico.CredencialCriada, $"Credencial {request.Tipo} criada.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _credenciais.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<CredencialResponse>.Ok(ToDto(credencial));
    }

    public async Task<Result<IReadOnlyList<CredencialResponse>>> ListByMoradorAsync(
        Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(morador?.Unidade?.Propriedade, proprietarioId))
        {
            return Result<IReadOnlyList<CredencialResponse>>.Fail("Morador não encontrado.");
        }

        var credenciais = await _credenciais.ListByMoradorAsync(moradorId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<CredencialResponse>>.Ok(credenciais.Select(ToDto).ToList());
    }

    public async Task<Result<CredencialResponse>> AtualizarStatusAsync(
        Guid proprietarioId, Guid credencialId, AtualizarStatusCredencialRequest request, CancellationToken cancellationToken)
    {
        var credencial = await _credenciais.GetByIdAsync(credencialId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(credencial?.Morador?.Unidade?.Propriedade, proprietarioId))
        {
            return Result<CredencialResponse>.Fail("Credencial não encontrada.");
        }

        var statusAnterior = credencial!.Status;
        if (statusAnterior != request.Status)
        {
            credencial.Status = request.Status;
            var tipoEvento = request.Status switch
            {
                StatusCredencial.Ativa => TipoEventoHistorico.CredencialReativada,
                StatusCredencial.Suspensa => TipoEventoHistorico.CredencialSuspensa,
                StatusCredencial.Expirada => TipoEventoHistorico.CredencialExpirada,
                StatusCredencial.Revogada => TipoEventoHistorico.CredencialRevogada,
                _ => TipoEventoHistorico.CredencialSuspensa,
            };
            await RegistrarHistoricoAsync(
                credencial.Id, tipoEvento, $"Status alterado de {statusAnterior} para {request.Status}.", proprietarioId, cancellationToken)
                .ConfigureAwait(false);
        }

        await _credenciais.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<CredencialResponse>.Ok(ToDto(credencial));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid credencialId, CancellationToken cancellationToken)
    {
        var credencial = await _credenciais.GetByIdAsync(credencialId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(credencial?.Morador?.Unidade?.Propriedade, proprietarioId))
        {
            return Result.Fail("Credencial não encontrada.");
        }

        var agora = DateTime.UtcNow;
        var permissoesDaCredencial = await _permissoes.ListByCredencialAsync(credencialId, cancellationToken).ConfigureAwait(false);

        credencial!.Excluido = true;
        credencial.DataExclusaoUtc = agora;
        credencial.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var permissao in permissoesDaCredencial)
        {
            permissao.Excluido = true;
            permissao.DataExclusaoUtc = agora;
            permissao.ExcluidoPorUsuarioId = proprietarioId;
        }

        await RegistrarHistoricoAsync(
            credencial.Id, TipoEventoHistorico.CredencialExcluida, "Credencial excluída.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _credenciais.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    private async Task RegistrarHistoricoAsync(
        Guid credencialId, TipoEventoHistorico tipoEvento, string descricao, Guid usuarioId, CancellationToken cancellationToken)
    {
        await _historico.AddAsync(
            new HistoricoCredencial
            {
                Id = Guid.NewGuid(),
                CredencialId = credencialId,
                TipoEvento = tipoEvento,
                Descricao = descricao,
                UsuarioId = usuarioId,
                CreatedAtUtc = DateTime.UtcNow,
            },
            cancellationToken).ConfigureAwait(false);
    }

    // Mesma mensagem para "nao existe" e "existe mas nao e do usuario" (padrao ja
    // usado desde a Sprint 6) — nao revela para o cliente que um registro de outro
    // dono existe com este Id.
    private static bool PertenceAoProprietario(Propriedade? propriedade, Guid proprietarioId) =>
        propriedade is not null && propriedade.ProprietarioId == proprietarioId;

    private static CredencialResponse ToDto(Credencial credencial) => new()
    {
        Id = credencial.Id,
        MoradorId = credencial.MoradorId,
        Tipo = credencial.Tipo,
        Status = credencial.Status,
    };
}
