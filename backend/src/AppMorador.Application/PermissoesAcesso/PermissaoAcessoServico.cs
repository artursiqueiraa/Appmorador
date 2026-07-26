using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.PermissoesAcesso;

/// <summary>
/// Ownership resolvido via Credencial (para Create/List) ou via PermissaoAcesso→
/// Credencial (para Update/Delete) — mesma cadeia até Propriedade.ProprietarioId já
/// usada em todo o domínio principal. Valida que o PontoAcesso pertence à mesma
/// Propriedade da Credencial antes de vincular (nunca cruza propriedades).
/// </summary>
public sealed class PermissaoAcessoServico : IPermissaoAcessoServico
{
    private readonly ICredencialRepositorio _credenciais;
    private readonly IPontoAcessoRepositorio _pontosAcesso;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IHistoricoCredencialRepositorio _historico;

    public PermissaoAcessoServico(
        ICredencialRepositorio credenciais,
        IPontoAcessoRepositorio pontosAcesso,
        IPermissaoAcessoRepositorio permissoes,
        IHistoricoCredencialRepositorio historico)
    {
        _credenciais = credenciais;
        _pontosAcesso = pontosAcesso;
        _permissoes = permissoes;
        _historico = historico;
    }

    public async Task<Result<PermissaoAcessoResponse>> CreateAsync(
        Guid proprietarioId, Guid credencialId, CriarPermissaoAcessoRequest request, CancellationToken cancellationToken)
    {
        var credencial = await _credenciais.GetByIdAsync(credencialId, cancellationToken).ConfigureAwait(false);
        var propriedadeDaCredencial = credencial?.Morador?.Unidade?.Propriedade;
        if (propriedadeDaCredencial is null || propriedadeDaCredencial.ProprietarioId != proprietarioId)
        {
            return Result<PermissaoAcessoResponse>.Fail("Credencial não encontrada.");
        }

        var pontoAcesso = await _pontosAcesso.GetByIdAsync(request.PontoAcessoId, cancellationToken).ConfigureAwait(false);
        if (pontoAcesso is null || pontoAcesso.PropriedadeId != propriedadeDaCredencial.Id)
        {
            // Mesma mensagem generica de "nao encontrado" — nao revela se o ponto
            // existe em outra propriedade (evita vazar dado de outro dono).
            return Result<PermissaoAcessoResponse>.Fail("Ponto de acesso não encontrado.");
        }

        var permissao = new PermissaoAcesso
        {
            Id = Guid.NewGuid(),
            CredencialId = credencialId,
            PontoAcessoId = request.PontoAcessoId,
            DiasPermitidos = request.DiasPermitidos ?? DiaSemana.Todos,
            HorarioInicial = request.HorarioInicial,
            HorarioFinal = request.HorarioFinal,
            DataInicial = request.DataInicial,
            DataFinal = request.DataFinal,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _permissoes.AddAsync(permissao, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(
            credencialId, TipoEventoHistorico.PermissaoCriada, $"Permissão criada para o ponto \"{pontoAcesso.Nome}\".", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _permissoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PermissaoAcessoResponse>.Ok(ToDto(permissao, pontoAcesso.Nome));
    }

    public async Task<Result<IReadOnlyList<PermissaoAcessoResponse>>> ListByCredencialAsync(
        Guid proprietarioId, Guid credencialId, CancellationToken cancellationToken)
    {
        var credencial = await _credenciais.GetByIdAsync(credencialId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(credencial?.Morador?.Unidade?.Propriedade, proprietarioId))
        {
            return Result<IReadOnlyList<PermissaoAcessoResponse>>.Fail("Credencial não encontrada.");
        }

        var permissoes = await _permissoes.ListByCredencialAsync(credencialId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<PermissaoAcessoResponse>>.Ok(
            permissoes.Select(p => ToDto(p, p.PontoAcesso?.Nome ?? "")).ToList());
    }

    public async Task<Result<PermissaoAcessoResponse>> UpdateAsync(
        Guid proprietarioId, Guid permissaoId, AtualizarPermissaoAcessoRequest request, CancellationToken cancellationToken)
    {
        var permissao = await _permissoes.GetByIdAsync(permissaoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(permissao?.Credencial?.Morador?.Unidade?.Propriedade, proprietarioId))
        {
            return Result<PermissaoAcessoResponse>.Fail("Permissão não encontrada.");
        }

        permissao!.DiasPermitidos = request.DiasPermitidos ?? DiaSemana.Todos;
        permissao.HorarioInicial = request.HorarioInicial;
        permissao.HorarioFinal = request.HorarioFinal;
        permissao.DataInicial = request.DataInicial;
        permissao.DataFinal = request.DataFinal;

        await RegistrarHistoricoAsync(
            permissao.CredencialId, TipoEventoHistorico.PermissaoAlterada,
            $"Regras de acesso alteradas para o ponto \"{permissao.PontoAcesso?.Nome}\".", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _permissoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PermissaoAcessoResponse>.Ok(ToDto(permissao, permissao.PontoAcesso?.Nome ?? ""));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid permissaoId, CancellationToken cancellationToken)
    {
        var permissao = await _permissoes.GetByIdAsync(permissaoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(permissao?.Credencial?.Morador?.Unidade?.Propriedade, proprietarioId))
        {
            return Result.Fail("Permissão não encontrada.");
        }

        permissao!.Excluido = true;
        permissao.DataExclusaoUtc = DateTime.UtcNow;
        permissao.ExcluidoPorUsuarioId = proprietarioId;

        await RegistrarHistoricoAsync(
            permissao.CredencialId, TipoEventoHistorico.PermissaoExcluida,
            $"Permissão removida do ponto \"{permissao.PontoAcesso?.Nome}\".", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _permissoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

    private static bool PertenceAoProprietario(Propriedade? propriedade, Guid proprietarioId) =>
        propriedade is not null && propriedade.ProprietarioId == proprietarioId;

    private static PermissaoAcessoResponse ToDto(PermissaoAcesso permissao, string pontoAcessoNome) => new()
    {
        Id = permissao.Id,
        CredencialId = permissao.CredencialId,
        PontoAcessoId = permissao.PontoAcessoId,
        PontoAcessoNome = pontoAcessoNome,
        DiasPermitidos = permissao.DiasPermitidos,
        HorarioInicial = permissao.HorarioInicial,
        HorarioFinal = permissao.HorarioFinal,
        DataInicial = permissao.DataInicial,
        DataFinal = permissao.DataFinal,
    };
}
