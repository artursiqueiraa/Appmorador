using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.PermissoesVeiculares;

/// <summary>
/// Ownership resolvido via Veiculo→Morador→Unidade→Propriedade.ProprietarioId. Valida
/// que o PontoAcesso pertence à mesma Propriedade do Veículo E tem
/// <see cref="TipoPontoAcesso.Veicular"/> — reaproveita a infraestrutura de Pontos de
/// Acesso da Sprint 7 em vez de um enum próprio de áreas (ver ADR 0012).
/// </summary>
public sealed class PermissaoVeicularServico : IPermissaoVeicularServico
{
    private readonly IVeiculoRepositorio _veiculos;
    private readonly IPontoAcessoRepositorio _pontosAcesso;
    private readonly IPermissaoVeicularRepositorio _permissoes;
    private readonly IHistoricoVeiculoRepositorio _historico;

    public PermissaoVeicularServico(
        IVeiculoRepositorio veiculos,
        IPontoAcessoRepositorio pontosAcesso,
        IPermissaoVeicularRepositorio permissoes,
        IHistoricoVeiculoRepositorio historico)
    {
        _veiculos = veiculos;
        _pontosAcesso = pontosAcesso;
        _permissoes = permissoes;
        _historico = historico;
    }

    public async Task<Result<PermissaoVeicularResponse>> CreateAsync(
        Guid proprietarioId, Guid veiculoId, CriarPermissaoVeicularRequest request, CancellationToken cancellationToken)
    {
        var veiculo = await _veiculos.GetByIdAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (veiculo?.Morador?.Unidade?.Propriedade is null || veiculo.Morador.Unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<PermissaoVeicularResponse>.Fail("Veículo não encontrado.");
        }

        var pontoAcesso = await _pontosAcesso.GetByIdAsync(request.PontoAcessoId, cancellationToken).ConfigureAwait(false);
        if (pontoAcesso is null || pontoAcesso.PropriedadeId != veiculo.Morador.Unidade.PropriedadeId || pontoAcesso.Tipo != TipoPontoAcesso.Veicular)
        {
            // Mesma mensagem generica de "nao encontrado" — nao revela se o ponto
            // existe (em outra propriedade, ou como tipo Geral).
            return Result<PermissaoVeicularResponse>.Fail("Ponto de acesso não encontrado.");
        }

        var permissao = new PermissaoVeicular
        {
            Id = Guid.NewGuid(),
            VeiculoId = veiculoId,
            PontoAcessoId = request.PontoAcessoId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _permissoes.AddAsync(permissao, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(
            veiculoId, $"Permissão concedida para o ponto \"{pontoAcesso.Nome}\".", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _permissoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PermissaoVeicularResponse>.Ok(ToDto(permissao, pontoAcesso.Nome));
    }

    public async Task<Result<IReadOnlyList<PermissaoVeicularResponse>>> ListByVeiculoAsync(
        Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken)
    {
        var veiculo = await _veiculos.GetByIdAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (veiculo?.Morador?.Unidade?.Propriedade is null || veiculo.Morador.Unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<PermissaoVeicularResponse>>.Fail("Veículo não encontrado.");
        }

        var permissoes = await _permissoes.ListByVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<PermissaoVeicularResponse>>.Ok(
            permissoes.Select(p => ToDto(p, p.PontoAcesso?.Nome ?? "")).ToList());
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid permissaoId, CancellationToken cancellationToken)
    {
        var permissao = await _permissoes.GetByIdAsync(permissaoId, cancellationToken).ConfigureAwait(false);
        if (permissao?.Veiculo?.Morador?.Unidade?.Propriedade is null || permissao.Veiculo.Morador.Unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result.Fail("Permissão não encontrada.");
        }

        permissao.Excluido = true;
        permissao.DataExclusaoUtc = DateTime.UtcNow;
        permissao.ExcluidoPorUsuarioId = proprietarioId;

        await RegistrarHistoricoAsync(
            permissao.VeiculoId, $"Permissão removida do ponto \"{permissao.PontoAcesso?.Nome}\".", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _permissoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    private async Task RegistrarHistoricoAsync(Guid veiculoId, string descricao, Guid usuarioId, CancellationToken cancellationToken) =>
        await _historico.AddAsync(
            new HistoricoVeiculo
            {
                Id = Guid.NewGuid(),
                VeiculoId = veiculoId,
                TipoEvento = TipoEventoHistoricoVeiculo.PermissaoVeicularAlterada,
                Descricao = descricao,
                UsuarioId = usuarioId,
                CreatedAtUtc = DateTime.UtcNow,
            },
            cancellationToken).ConfigureAwait(false);

    private static PermissaoVeicularResponse ToDto(PermissaoVeicular permissao, string pontoAcessoNome) => new()
    {
        Id = permissao.Id,
        VeiculoId = permissao.VeiculoId,
        PontoAcessoId = permissao.PontoAcessoId,
        PontoAcessoNome = pontoAcessoNome,
    };
}
