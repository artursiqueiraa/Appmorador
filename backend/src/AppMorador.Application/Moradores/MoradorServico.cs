using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Moradores;

/// <summary>
/// Ownership resolvido subindo a cadeia Morador → Unidade → Propriedade.ProprietarioId
/// — mesmo padrão já usado em <see cref="Application.Unidades.UnidadeServico"/>, nunca
/// um mecanismo de autorização novo.
/// </summary>
public sealed class MoradorServico : IMoradorServico
{
    private readonly IUnidadeRepositorio _unidades;
    private readonly IMoradorRepositorio _moradores;
    private readonly ICredencialRepositorio _credenciais;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IAutorizacaoRepositorio _autorizacoes;
    private readonly IVeiculoRepositorio _veiculos;
    private readonly IVinculoVeiculoVagaRepositorio _vinculosVeiculoVaga;
    private readonly IPermissaoVeicularRepositorio _permissoesVeiculares;
    private readonly IEntregaRepositorio _entregas;

    public MoradorServico(
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

    public async Task<Result<MoradorResponse>> CreateAsync(
        Guid proprietarioId, Guid unidadeId, CriarMoradorRequest request, CancellationToken cancellationToken)
    {
        var unidade = await _unidades.GetByIdAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        if (unidade is null || unidade.Propriedade is null || unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<MoradorResponse>.Fail("Unidade não encontrada.");
        }

        var morador = new Morador
        {
            Id = Guid.NewGuid(),
            UnidadeId = unidadeId,
            Nome = request.Nome.Trim(),
            Telefone = NullIfBlank(request.Telefone),
            Email = NullIfBlank(request.Email),
            Documento = NullIfBlank(request.Documento),
            Status = StatusMorador.Ativo,
            Observacoes = NullIfBlank(request.Observacoes),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _moradores.AddAsync(morador, cancellationToken).ConfigureAwait(false);
        await _moradores.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MoradorResponse>.Ok(ToDto(morador));
    }

    public async Task<Result<IReadOnlyList<MoradorResponse>>> ListByUnidadeAsync(
        Guid proprietarioId, Guid unidadeId, CancellationToken cancellationToken)
    {
        var unidade = await _unidades.GetByIdAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        if (unidade is null || unidade.Propriedade is null || unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<MoradorResponse>>.Fail("Unidade não encontrada.");
        }

        var moradores = await _moradores.ListByUnidadeAsync(unidadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<MoradorResponse>>.Ok(moradores.Select(ToDto).ToList());
    }

    public async Task<Result<MoradorResponse>> UpdateAsync(
        Guid proprietarioId, Guid moradorId, AtualizarMoradorRequest request, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAo(morador, proprietarioId))
        {
            return Result<MoradorResponse>.Fail("Morador não encontrado.");
        }

        morador!.Nome = request.Nome.Trim();
        morador.Telefone = NullIfBlank(request.Telefone);
        morador.Email = NullIfBlank(request.Email);
        morador.Documento = NullIfBlank(request.Documento);
        morador.Status = request.Status;
        morador.Observacoes = NullIfBlank(request.Observacoes);
        await _moradores.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<MoradorResponse>.Ok(ToDto(morador));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAo(morador, proprietarioId))
        {
            return Result.Fail("Morador não encontrado.");
        }

        // Exclusao logica (ADR 0009) — o registro nunca sai do banco. Cascata
        // explicita para as Credenciais do Morador e as Permissoes delas (Sprint 7),
        // e para as Autorizacoes em que ele e o responsavel (Sprint 8) — sem
        // responsavel valido, a autorizacao perde a ancora de responsabilidade.
        var agora = DateTime.UtcNow;
        var credenciaisDoMorador = await _credenciais.ListByMoradorAsync(moradorId, cancellationToken).ConfigureAwait(false);
        var permissoesDoMorador = await _permissoes.ListByMoradorAsync(moradorId, cancellationToken).ConfigureAwait(false);
        var autorizacoesDoMorador = await _autorizacoes.ListByMoradorResponsavelAsync(moradorId, cancellationToken).ConfigureAwait(false);
        var veiculosDoMorador = await _veiculos.ListByMoradorAsync(moradorId, cancellationToken).ConfigureAwait(false);
        var idsVeiculosDoMorador = veiculosDoMorador.Select(v => v.Id).ToList();
        var vinculosVeiculoVagaDoMorador = await _vinculosVeiculoVaga.ListByVeiculosAsync(idsVeiculosDoMorador, cancellationToken).ConfigureAwait(false);
        var permissoesVeicularesDoMorador = await _permissoesVeiculares.ListByVeiculosAsync(idsVeiculosDoMorador, cancellationToken).ConfigureAwait(false);
        var entregasDoMorador = await _entregas.ListByMoradorAsync(moradorId, cancellationToken).ConfigureAwait(false);

        morador!.Excluido = true;
        morador.DataExclusaoUtc = agora;
        morador.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var credencial in credenciaisDoMorador)
        {
            credencial.Excluido = true;
            credencial.DataExclusaoUtc = agora;
            credencial.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissao in permissoesDoMorador)
        {
            permissao.Excluido = true;
            permissao.DataExclusaoUtc = agora;
            permissao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var autorizacao in autorizacoesDoMorador)
        {
            autorizacao.Excluido = true;
            autorizacao.DataExclusaoUtc = agora;
            autorizacao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var veiculo in veiculosDoMorador)
        {
            veiculo.Excluido = true;
            veiculo.DataExclusaoUtc = agora;
            veiculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var vinculo in vinculosVeiculoVagaDoMorador)
        {
            vinculo.Excluido = true;
            vinculo.DataExclusaoUtc = agora;
            vinculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissaoVeicular in permissoesVeicularesDoMorador)
        {
            permissaoVeicular.Excluido = true;
            permissaoVeicular.DataExclusaoUtc = agora;
            permissaoVeicular.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var entrega in entregasDoMorador)
        {
            entrega.Excluido = true;
            entrega.DataExclusaoUtc = agora;
            entrega.ExcluidoPorUsuarioId = proprietarioId;
        }

        await _moradores.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    // Mesma mensagem para "nao existe" e "existe mas nao e do usuario" (padrao ja
    // usado em PropriedadeServico/UnidadeServico) — nao revela para o cliente que um
    // morador de outro dono existe com este Id.
    private static bool PertenceAo(Morador? morador, Guid proprietarioId) =>
        morador is not null && morador.Unidade?.Propriedade is not null && morador.Unidade.Propriedade.ProprietarioId == proprietarioId;

    private static string? NullIfBlank(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static MoradorResponse ToDto(Morador morador) => new()
    {
        Id = morador.Id,
        UnidadeId = morador.UnidadeId,
        Nome = morador.Nome,
        FotoPath = morador.FotoPath,
        Telefone = morador.Telefone,
        Email = morador.Email,
        Documento = morador.Documento,
        Status = morador.Status,
        Observacoes = morador.Observacoes,
    };
}
