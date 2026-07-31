using AppMorador.Application.Auditoria;
using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Painel.VinculosEquipamento;

public sealed class VinculoEquipamentoServico : IVinculoEquipamentoServico
{
    private readonly IVinculoEquipamentoPropriedadeRepositorio _vinculos;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IAuditoriaService _auditoria;

    public VinculoEquipamentoServico(
        IVinculoEquipamentoPropriedadeRepositorio vinculos, IEquipamentoRepositorio equipamentos,
        IPropriedadeRepositorio propriedades, IUsuarioRepositorio usuarios, IAuditoriaService auditoria)
    {
        _vinculos = vinculos;
        _equipamentos = equipamentos;
        _propriedades = propriedades;
        _usuarios = usuarios;
        _auditoria = auditoria;
    }

    private async Task<string> ObterNomeUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        return usuario?.Nome ?? "(desconhecido)";
    }

    public async Task<VinculosPaginadosResponse> ListarAtivosAsync(int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        pagina = pagina <= 0 ? 1 : pagina;
        tamanhoPagina = tamanhoPagina is <= 0 or > 100 ? 20 : tamanhoPagina;

        var (itens, total) = await _vinculos.ListarAtivosGlobalAsync(pagina, tamanhoPagina, cancellationToken).ConfigureAwait(false);

        return new VinculosPaginadosResponse
        {
            Itens = itens.Select(ToDto).ToList(),
            PaginaAtual = pagina,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanhoPagina),
            TotalItens = total,
        };
    }

    public async Task<Result<IReadOnlyList<VinculoResponse>>> ListarHistoricoAsync(Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<IReadOnlyList<VinculoResponse>>.Fail("Equipamento não encontrado.");
        }

        var historico = await _vinculos.ListarHistoricoPorEquipamentoAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<VinculoResponse>>.Ok(historico.Select(ToDto).ToList());
    }

    public async Task<Result<VinculoResponse>> ProvisionarAsync(
        Guid usuarioId, ProvisionarEquipamentoRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(request.EquipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<VinculoResponse>.Fail("Equipamento não encontrado.");
        }

        var propriedade = await _propriedades.GetByIdAsync(request.PropriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<VinculoResponse>.Fail("Propriedade não encontrada.");
        }

        var vinculoAtivo = await _vinculos.GetVinculoAtivoPorEquipamentoAsync(request.EquipamentoId, cancellationToken).ConfigureAwait(false);
        if (vinculoAtivo is not null)
        {
            return Result<VinculoResponse>.Fail("Este equipamento já está provisionado em outra propriedade. Troque ou desvincule primeiro.");
        }

        var vinculo = new VinculoEquipamentoPropriedade
        {
            Id = Guid.NewGuid(),
            EquipamentoId = request.EquipamentoId,
            Equipamento = equipamento,
            PropriedadeId = request.PropriedadeId,
            Propriedade = propriedade,
            DataInicioUtc = DateTime.UtcNow,
            CriadoPorUsuarioId = usuarioId,
            Observacoes = string.IsNullOrWhiteSpace(request.Observacoes) ? null : request.Observacoes.Trim(),
        };

        await _vinculos.AddAsync(vinculo, cancellationToken).ConfigureAwait(false);
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var usuarioNome = await ObterNomeUsuarioAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        await _auditoria.RegistrarAsync(
            usuarioId, usuarioNome, TipoAcaoAuditoria.Criar, "VinculoEquipamentoPropriedade", vinculo.Id.ToString(),
            $"Equipamento {equipamento.Nome} provisionado em {propriedade.Nome}", ipAddress, cancellationToken).ConfigureAwait(false);

        return Result<VinculoResponse>.Ok(ToDto(vinculo));
    }

    public async Task<Result<VinculoResponse>> TrocarAsync(
        Guid usuarioId, TrocarEquipamentoRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var equipamentoAntigo = await _equipamentos.GetByIdAsync(request.EquipamentoAntigoId, cancellationToken).ConfigureAwait(false);
        if (equipamentoAntigo is null)
        {
            return Result<VinculoResponse>.Fail("Equipamento antigo não encontrado.");
        }

        var equipamentoNovo = await _equipamentos.GetByIdAsync(request.EquipamentoNovoId, cancellationToken).ConfigureAwait(false);
        if (equipamentoNovo is null)
        {
            return Result<VinculoResponse>.Fail("Equipamento novo não encontrado.");
        }

        var propriedade = await _propriedades.GetByIdAsync(request.PropriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<VinculoResponse>.Fail("Propriedade não encontrada.");
        }

        var vinculoAntigo = await _vinculos.GetVinculoAtivoPorEquipamentoAsync(request.EquipamentoAntigoId, cancellationToken).ConfigureAwait(false);
        if (vinculoAntigo is null || vinculoAntigo.PropriedadeId != request.PropriedadeId)
        {
            return Result<VinculoResponse>.Fail("O equipamento antigo não está provisionado nesta propriedade.");
        }

        var vinculoNovoExistente = await _vinculos.GetVinculoAtivoPorEquipamentoAsync(request.EquipamentoNovoId, cancellationToken).ConfigureAwait(false);
        if (vinculoNovoExistente is not null)
        {
            return Result<VinculoResponse>.Fail("O equipamento novo já está provisionado em outra propriedade.");
        }

        var agora = DateTime.UtcNow;

        // 1. Encerrar o provisionamento anterior.
        vinculoAntigo.DataFimUtc = agora;

        // 2. Histórico preservado (nunca apagamos o vínculo antigo — só marcamos o fim).
        // 3. Criar novo vínculo.
        var vinculoNovo = new VinculoEquipamentoPropriedade
        {
            Id = Guid.NewGuid(),
            EquipamentoId = request.EquipamentoNovoId,
            Equipamento = equipamentoNovo,
            PropriedadeId = request.PropriedadeId,
            Propriedade = propriedade,
            DataInicioUtc = agora,
            CriadoPorUsuarioId = usuarioId,
            Observacoes = string.IsNullOrWhiteSpace(request.Observacoes) ? null : request.Observacoes.Trim(),
        };

        await _vinculos.AddAsync(vinculoNovo, cancellationToken).ConfigureAwait(false);
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var usuarioNome = await ObterNomeUsuarioAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        await _auditoria.RegistrarAsync(
            usuarioId, usuarioNome, TipoAcaoAuditoria.Editar, "VinculoEquipamentoPropriedade", vinculoNovo.Id.ToString(),
            $"Troca em {propriedade.Nome}: {equipamentoAntigo.Nome} → {equipamentoNovo.Nome}", ipAddress, cancellationToken).ConfigureAwait(false);

        return Result<VinculoResponse>.Ok(ToDto(vinculoNovo));
    }

    public async Task<Result> DesvincularAsync(Guid usuarioId, Guid equipamentoId, string? ipAddress, CancellationToken cancellationToken)
    {
        var vinculoAtivo = await _vinculos.GetVinculoAtivoPorEquipamentoAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (vinculoAtivo is null)
        {
            return Result.Fail("Este equipamento não está provisionado em nenhuma propriedade.");
        }

        vinculoAtivo.DataFimUtc = DateTime.UtcNow;
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var usuarioNome = await ObterNomeUsuarioAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        await _auditoria.RegistrarAsync(
            usuarioId, usuarioNome, TipoAcaoAuditoria.Excluir, "VinculoEquipamentoPropriedade", vinculoAtivo.Id.ToString(),
            "Equipamento desvinculado", ipAddress, cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    public async Task<DashboardAlocacaoResponse> ObterDashboardAsync(CancellationToken cancellationToken)
    {
        var porStatus = await _equipamentos.ContarPorStatusGlobalAsync(cancellationToken).ConfigureAwait(false);
        var totalEquipamentos = porStatus.Values.Sum();
        var totalProvisionados = await _vinculos.ContarEquipamentosProvisionadosAsync(cancellationToken).ConfigureAwait(false);

        return new DashboardAlocacaoResponse
        {
            TotalEquipamentos = totalEquipamentos,
            TotalProvisionados = totalProvisionados,
            TotalDisponiveis = Math.Max(0, totalEquipamentos - totalProvisionados),
        };
    }

    private static VinculoResponse ToDto(VinculoEquipamentoPropriedade vinculo) => new()
    {
        Id = vinculo.Id,
        EquipamentoId = vinculo.EquipamentoId,
        EquipamentoNome = vinculo.Equipamento?.Nome,
        PropriedadeId = vinculo.PropriedadeId,
        PropriedadeNome = vinculo.Propriedade?.Nome,
        DataInicioUtc = vinculo.DataInicioUtc,
        DataFimUtc = vinculo.DataFimUtc,
        Ativo = vinculo.DataFimUtc is null,
        CriadoPorUsuarioId = vinculo.CriadoPorUsuarioId,
        Observacoes = vinculo.Observacoes,
    };
}
