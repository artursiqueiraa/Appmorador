using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Unidades;

/// <summary>
/// Mesmo padrão de ownership de <see cref="Application.Propriedades.PropriedadeServico"/>:
/// toda operação confirma que a Propriedade (ou, para update/delete, a Unidade via sua
/// Propriedade) pertence ao usuário autenticado antes de agir.
/// </summary>
public sealed class UnidadeServico : IUnidadeServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUnidadeRepositorio _unidades;
    private readonly IMoradorRepositorio _moradores;
    private readonly ICredencialRepositorio _credenciais;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IAutorizacaoRepositorio _autorizacoes;
    private readonly IVeiculoRepositorio _veiculos;
    private readonly IVinculoVeiculoVagaRepositorio _vinculosVeiculoVaga;
    private readonly IPermissaoVeicularRepositorio _permissoesVeiculares;
    private readonly IEntregaRepositorio _entregas;

    public UnidadeServico(
        IPropriedadeRepositorio propriedades,
        IUnidadeRepositorio unidades,
        IMoradorRepositorio moradores,
        ICredencialRepositorio credenciais,
        IPermissaoAcessoRepositorio permissoes,
        IAutorizacaoRepositorio autorizacoes,
        IVeiculoRepositorio veiculos,
        IVinculoVeiculoVagaRepositorio vinculosVeiculoVaga,
        IPermissaoVeicularRepositorio permissoesVeiculares,
        IEntregaRepositorio entregas)
    {
        _propriedades = propriedades;
        _unidades = unidades;
        _moradores = moradores;
        _credenciais = credenciais;
        _permissoes = permissoes;
        _autorizacoes = autorizacoes;
        _veiculos = veiculos;
        _vinculosVeiculoVaga = vinculosVeiculoVaga;
        _permissoesVeiculares = permissoesVeiculares;
        _entregas = entregas;
    }

    public async Task<Result<UnidadeResponse>> CreateAsync(
        Guid proprietarioId, Guid propriedadeId, CriarUnidadeRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<UnidadeResponse>.Fail("Propriedade não encontrada.");
        }

        var unidade = new Unidade
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Tipo = request.Tipo,
            Identificacao = request.Identificacao.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _unidades.AddAsync(unidade, cancellationToken).ConfigureAwait(false);
        await _unidades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UnidadeResponse>.Ok(ToDto(unidade));
    }

    public async Task<Result<IReadOnlyList<UnidadeResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<UnidadeResponse>>.Fail("Propriedade não encontrada.");
        }

        var unidades = await _unidades.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<UnidadeResponse>>.Ok(unidades.Select(ToDto).ToList());
    }

    public async Task<Result<UnidadeResponse>> UpdateAsync(
        Guid proprietarioId, Guid unidadeId, AtualizarUnidadeRequest request, CancellationToken cancellationToken)
    {
        var unidade = await _unidades.GetByIdAsync(unidadeId, cancellationToken).ConfigureAwait(false);

        // Mesma mensagem para "nao existe" e "existe mas nao e do usuario" (padrao ja
        // usado em PropriedadeServico) — nao revela para o cliente que uma unidade de
        // outro dono existe com este Id.
        if (unidade is null || unidade.Propriedade is null || unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<UnidadeResponse>.Fail("Unidade não encontrada.");
        }

        unidade.Tipo = request.Tipo;
        unidade.Identificacao = request.Identificacao.Trim();
        await _unidades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<UnidadeResponse>.Ok(ToDto(unidade));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid unidadeId, CancellationToken cancellationToken)
    {
        var unidade = await _unidades.GetByIdAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        if (unidade is null || unidade.Propriedade is null || unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result.Fail("Unidade não encontrada.");
        }

        // Exclusao logica (ADR 0009): a Unidade some da experiencia do usuario, mas a
        // linha continua no banco. Cascata explicita para os Moradores dela e para
        // as Credenciais/Permissoes deles (Sprint 7) — soft delete nao tem cascade no
        // banco (Restrict), entao a aplicacao e responsavel por manter tudo consistente.
        var agora = DateTime.UtcNow;
        var moradoresDaUnidade = await _moradores.ListByUnidadeAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        var credenciaisDaUnidade = await _credenciais.ListByUnidadeAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        var permissoesDaUnidade = await _permissoes.ListByUnidadeAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        var autorizacoesDaUnidade = await _autorizacoes.ListByUnidadeAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        var veiculosDaUnidade = await _veiculos.ListByUnidadeAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        var idsVeiculosDaUnidade = veiculosDaUnidade.Select(v => v.Id).ToList();
        var vinculosVeiculoVagaDaUnidade = await _vinculosVeiculoVaga.ListByVeiculosAsync(idsVeiculosDaUnidade, cancellationToken).ConfigureAwait(false);
        var permissoesVeicularesDaUnidade = await _permissoesVeiculares.ListByVeiculosAsync(idsVeiculosDaUnidade, cancellationToken).ConfigureAwait(false);
        var entregasDaUnidade = await _entregas.ListByUnidadeAsync(unidadeId, cancellationToken).ConfigureAwait(false);

        unidade.Excluido = true;
        unidade.DataExclusaoUtc = agora;
        unidade.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var morador in moradoresDaUnidade)
        {
            morador.Excluido = true;
            morador.DataExclusaoUtc = agora;
            morador.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var credencial in credenciaisDaUnidade)
        {
            credencial.Excluido = true;
            credencial.DataExclusaoUtc = agora;
            credencial.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissao in permissoesDaUnidade)
        {
            permissao.Excluido = true;
            permissao.DataExclusaoUtc = agora;
            permissao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var autorizacao in autorizacoesDaUnidade)
        {
            autorizacao.Excluido = true;
            autorizacao.DataExclusaoUtc = agora;
            autorizacao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var veiculo in veiculosDaUnidade)
        {
            veiculo.Excluido = true;
            veiculo.DataExclusaoUtc = agora;
            veiculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var vinculo in vinculosVeiculoVagaDaUnidade)
        {
            vinculo.Excluido = true;
            vinculo.DataExclusaoUtc = agora;
            vinculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissaoVeicular in permissoesVeicularesDaUnidade)
        {
            permissaoVeicular.Excluido = true;
            permissaoVeicular.DataExclusaoUtc = agora;
            permissaoVeicular.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var entrega in entregasDaUnidade)
        {
            entrega.Excluido = true;
            entrega.DataExclusaoUtc = agora;
            entrega.ExcluidoPorUsuarioId = proprietarioId;
        }

        await _unidades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private static UnidadeResponse ToDto(Unidade unidade) => new()
    {
        Id = unidade.Id,
        PropriedadeId = unidade.PropriedadeId,
        Tipo = unidade.Tipo,
        Identificacao = unidade.Identificacao,
    };
}
