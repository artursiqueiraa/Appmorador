using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Propriedades;

public sealed class PropriedadeServico : IPropriedadeServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUnidadeRepositorio _unidades;
    private readonly IMoradorRepositorio _moradores;
    private readonly ICredencialRepositorio _credenciais;
    private readonly IPontoAcessoRepositorio _pontosAcesso;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IVisitanteRepositorio _visitantes;
    private readonly IAutorizacaoRepositorio _autorizacoes;
    private readonly IVeiculoRepositorio _veiculos;
    private readonly IVagaRepositorio _vagas;
    private readonly IVinculoVeiculoVagaRepositorio _vinculosVeiculoVaga;
    private readonly IPermissaoVeicularRepositorio _permissoesVeiculares;
    private readonly IEntregaRepositorio _entregas;
    private readonly IEquipamentoRepositorio _equipamentos;

    public PropriedadeServico(
        IPropriedadeRepositorio propriedades,
        IUnidadeRepositorio unidades,
        IMoradorRepositorio moradores,
        ICredencialRepositorio credenciais,
        IPontoAcessoRepositorio pontosAcesso,
        IPermissaoAcessoRepositorio permissoes,
        IVisitanteRepositorio visitantes,
        IAutorizacaoRepositorio autorizacoes,
        IVeiculoRepositorio veiculos,
        IVagaRepositorio vagas,
        IVinculoVeiculoVagaRepositorio vinculosVeiculoVaga,
        IPermissaoVeicularRepositorio permissoesVeiculares,
        IEntregaRepositorio entregas,
        IEquipamentoRepositorio equipamentos)
    {
        _propriedades = propriedades;
        _unidades = unidades;
        _moradores = moradores;
        _credenciais = credenciais;
        _pontosAcesso = pontosAcesso;
        _permissoes = permissoes;
        _visitantes = visitantes;
        _autorizacoes = autorizacoes;
        _veiculos = veiculos;
        _vagas = vagas;
        _vinculosVeiculoVaga = vinculosVeiculoVaga;
        _permissoesVeiculares = permissoesVeiculares;
        _entregas = entregas;
        _equipamentos = equipamentos;
    }

    public async Task<PropriedadeResponse> CreateAsync(Guid proprietarioId, CriarPropriedadeRequest request, CancellationToken cancellationToken)
    {
        var propriedade = new Propriedade
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Tipo = request.Tipo,
            Endereco = request.Endereco?.Trim(),
            ProprietarioId = proprietarioId,
        };

        await _propriedades.AddAsync(propriedade, cancellationToken).ConfigureAwait(false);
        await _propriedades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToDto(propriedade);
    }

    public async Task<IReadOnlyList<PropriedadeResponse>> ListByOwnerAsync(Guid proprietarioId, CancellationToken cancellationToken)
    {
        var propriedades = await _propriedades.ListByOwnerAsync(proprietarioId, cancellationToken).ConfigureAwait(false);
        return propriedades.Select(ToDto).ToList();
    }

    public async Task<Result<PropriedadeResponse>> UpdateAsync(
        Guid proprietarioId, Guid propriedadeId, AtualizarPropriedadeRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        // Mesma mensagem para "nao existe" e "existe mas nao e do usuario" — nao
        // revela para o cliente que uma propriedade de outro dono existe com este Id.
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<PropriedadeResponse>.Fail("Propriedade não encontrada.");
        }

        propriedade.Nome = request.Nome.Trim();
        propriedade.Tipo = request.Tipo;
        propriedade.Endereco = request.Endereco?.Trim();
        await _propriedades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PropriedadeResponse>.Ok(ToDto(propriedade));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result.Fail("Propriedade não encontrada.");
        }

        // Exclusao logica (ADR 0009): a Propriedade some da experiencia do usuario,
        // mas nunca sai fisicamente do banco (auditoria/restauracao futura). Cascade
        // explicito para todo o agregado — Unidades, Moradores, Credenciais,
        // Permissoes e Pontos de Acesso (Sprint 7), Visitantes e Autorizacoes
        // (Sprint 8), Veiculos, Vagas, Vinculos e Permissoes Veiculares (Sprint 9),
        // Entregas (Sprint 10), Equipamentos (Sprint 11) — soft delete nao tem cascade
        // no banco (FKs sao Restrict), entao a aplicacao mantem tudo consistente.
        var agora = DateTime.UtcNow;
        var unidadesDaPropriedade = await _unidades.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var moradoresDaPropriedade = await _moradores.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var credenciaisDaPropriedade = await _credenciais.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var permissoesDaPropriedade = await _permissoes.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var pontosAcessoDaPropriedade = await _pontosAcesso.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var visitantesDaPropriedade = await _visitantes.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var autorizacoesDaPropriedade = await _autorizacoes.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var veiculosDaPropriedade = await _veiculos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var vagasDaPropriedade = await _vagas.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var vinculosVeiculoVagaDaPropriedade = await _vinculosVeiculoVaga.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var permissoesVeicularesDaPropriedade = await _permissoesVeiculares.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var entregasDaPropriedade = await _entregas.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var equipamentosDaPropriedade = await _equipamentos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        propriedade.Excluido = true;
        propriedade.DataExclusaoUtc = agora;
        propriedade.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var unidade in unidadesDaPropriedade)
        {
            unidade.Excluido = true;
            unidade.DataExclusaoUtc = agora;
            unidade.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var morador in moradoresDaPropriedade)
        {
            morador.Excluido = true;
            morador.DataExclusaoUtc = agora;
            morador.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var credencial in credenciaisDaPropriedade)
        {
            credencial.Excluido = true;
            credencial.DataExclusaoUtc = agora;
            credencial.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissao in permissoesDaPropriedade)
        {
            permissao.Excluido = true;
            permissao.DataExclusaoUtc = agora;
            permissao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var ponto in pontosAcessoDaPropriedade)
        {
            ponto.Excluido = true;
            ponto.DataExclusaoUtc = agora;
            ponto.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var visitante in visitantesDaPropriedade)
        {
            visitante.Excluido = true;
            visitante.DataExclusaoUtc = agora;
            visitante.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var autorizacao in autorizacoesDaPropriedade)
        {
            autorizacao.Excluido = true;
            autorizacao.DataExclusaoUtc = agora;
            autorizacao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var veiculo in veiculosDaPropriedade)
        {
            veiculo.Excluido = true;
            veiculo.DataExclusaoUtc = agora;
            veiculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var vaga in vagasDaPropriedade)
        {
            vaga.Excluido = true;
            vaga.DataExclusaoUtc = agora;
            vaga.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var vinculo in vinculosVeiculoVagaDaPropriedade)
        {
            vinculo.Excluido = true;
            vinculo.DataExclusaoUtc = agora;
            vinculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissaoVeicular in permissoesVeicularesDaPropriedade)
        {
            permissaoVeicular.Excluido = true;
            permissaoVeicular.DataExclusaoUtc = agora;
            permissaoVeicular.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var entrega in entregasDaPropriedade)
        {
            entrega.Excluido = true;
            entrega.DataExclusaoUtc = agora;
            entrega.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var equipamento in equipamentosDaPropriedade)
        {
            equipamento.Excluido = true;
            equipamento.DataExclusaoUtc = agora;
            equipamento.ExcluidoPorUsuarioId = proprietarioId;
        }

        await _propriedades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private static PropriedadeResponse ToDto(Propriedade propriedade) => new()
    {
        Id = propriedade.Id,
        Nome = propriedade.Nome,
        Tipo = propriedade.Tipo,
        Endereco = propriedade.Endereco,
    };
}
