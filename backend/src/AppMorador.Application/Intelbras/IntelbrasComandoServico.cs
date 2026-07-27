using AppMorador.Application.Common;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Intelbras;

public sealed class IntelbrasComandoServico : IIntelbrasComandoServico
{
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IEventoEquipamentoRepositorio _eventosEquipamento;
    private readonly ICriptografiaSimetrica _criptografia;
    private readonly IIntelbrasProvider _intelbrasProvider;
    private readonly ISnapshotOperacionalServico _snapshotOperacional;

    public IntelbrasComandoServico(
        IEquipamentoRepositorio equipamentos,
        IEventoEquipamentoRepositorio eventosEquipamento,
        ICriptografiaSimetrica criptografia,
        IIntelbrasProvider intelbrasProvider,
        ISnapshotOperacionalServico snapshotOperacional)
    {
        _equipamentos = equipamentos;
        _eventosEquipamento = eventosEquipamento;
        _criptografia = criptografia;
        _intelbrasProvider = intelbrasProvider;
        _snapshotOperacional = snapshotOperacional;
    }

    public async Task<Result<CentralIntelbrasResponse>> ObterDetalhesAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoIntelbrasAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<CentralIntelbrasResponse>.Fail("Central Intelbras não encontrada.");
        }

        return Result<CentralIntelbrasResponse>.Ok(ToDto(equipamento, null));
    }

    public async Task<Result<ResultadoTesteConexaoIntelbras>> TestarConexaoAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoIntelbrasAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<ResultadoTesteConexaoIntelbras>.Fail("Central Intelbras não encontrada.");
        }

        var conexao = MontarConexao(equipamento);
        var resultado = await _intelbrasProvider.TestarConexaoAsync(conexao, cancellationToken).ConfigureAwait(false);

        equipamento.Status = resultado.Sucesso ? StatusEquipamento.Online : StatusEquipamento.Offline;
        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);

        return Result<ResultadoTesteConexaoIntelbras>.Ok(resultado);
    }

    public Task<Result<ResultadoComandoIntelbras>> ConsultarStatusAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (conexao, ct) => _intelbrasProvider.ConsultarStatusAsync(conexao, ct), cancellationToken);

    public Task<Result<ResultadoComandoIntelbras>> ArmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (conexao, ct) => _intelbrasProvider.ArmarAsync(conexao, particao, ct), cancellationToken);

    public Task<Result<ResultadoComandoIntelbras>> DesarmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (conexao, ct) => _intelbrasProvider.DesarmarAsync(conexao, particao, ct), cancellationToken);

    public async Task<Result<ImportacaoEventosIntelbrasResponse>> ImportarEventosAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoIntelbrasAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<ImportacaoEventosIntelbrasResponse>.Fail("Central Intelbras não encontrada.");
        }

        try
        {
            var importados = await _intelbrasProvider.ImportarEventosAsync(MontarConexao(equipamento), cancellationToken).ConfigureAwait(false);
            equipamento.Status = StatusEquipamento.Online;

            var agora = DateTime.UtcNow;
            var novosEventos = importados
                .Select(e => new EventoEquipamento
                {
                    Id = Guid.NewGuid(),
                    EquipamentoId = equipamento.Id,
                    CodigoEventoOriginal = e.CodigoEventoOriginal,
                    Descricao = e.Descricao,
                    OcorridoEmUtc = e.OcorridoEmUtc,
                    CreatedAtUtc = agora,
                })
                .ToList();

            if (novosEventos.Count > 0)
            {
                await _eventosEquipamento.AddRangeAsync(novosEventos, cancellationToken).ConfigureAwait(false);
            }

            await _eventosEquipamento.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);

            return Result<ImportacaoEventosIntelbrasResponse>.Ok(new ImportacaoEventosIntelbrasResponse { QuantidadeImportada = novosEventos.Count });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            equipamento.Status = StatusEquipamento.Offline;
            await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
            return Result<ImportacaoEventosIntelbrasResponse>.Fail($"Não foi possível importar eventos da central: {ex.Message}");
        }
    }

    private async Task<Result<ResultadoComandoIntelbras>> ExecutarComandoAsync(
        Guid proprietarioId,
        Guid equipamentoId,
        Func<ConexaoIntelbras, CancellationToken, Task<ResultadoComandoIntelbras>> executar,
        CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoIntelbrasAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<ResultadoComandoIntelbras>.Fail("Central Intelbras não encontrada.");
        }

        var resultado = await executar(MontarConexao(equipamento), cancellationToken).ConfigureAwait(false);

        equipamento.Status = resultado.Sucesso ? StatusEquipamento.Online : StatusEquipamento.Offline;
        if (resultado.Sucesso)
        {
            equipamento.UltimaSincronizacaoUtc = DateTime.UtcNow;
        }

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);

        return Result<ResultadoComandoIntelbras>.Ok(resultado);
    }

    // Sprint 15 (ADR 0018) — mesmo ponto de integração com a Sprint 14 (ADR 0017) já
    // usado por EquipamentoIntegracaoServico/JflComandoServico: nenhuma alteração no
    // fluxo Snapshot→Publicador→SignalR foi necessária para o terceiro fabricante.
    private Task PublicarAtualizacaoAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _snapshotOperacional.RegenerarEPublicarAsync(propriedadeId, MotivoAtualizacaoOperacional.EquipamentoStatusAlterado, cancellationToken);

    private async Task<Equipamento?> ResolverEquipamentoIntelbrasAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento?.Propriedade is null || equipamento.Propriedade.ProprietarioId != proprietarioId)
        {
            return null;
        }

        return equipamento.Fabricante == FabricanteEquipamento.Intelbras
            && !string.IsNullOrWhiteSpace(equipamento.Ip)
            && equipamento.Porta is not null
            ? equipamento
            : null;
    }

    private ConexaoIntelbras MontarConexao(Equipamento equipamento) => new()
    {
        Ip = equipamento.Ip!,
        Porta = equipamento.Porta!.Value,
        Senha = _criptografia.Descriptografar(equipamento.SenhaCriptografada!),
    };

    private static CentralIntelbrasResponse ToDto(Equipamento equipamento, StatusCentralIntelbrasInfo? statusResultante) => new()
    {
        EquipamentoId = equipamento.Id,
        PropriedadeId = equipamento.PropriedadeId,
        Nome = equipamento.Nome,
        Modelo = equipamento.ModeloEquipamento?.Nome,
        Status = equipamento.Status,
        UltimaSincronizacaoUtc = equipamento.UltimaSincronizacaoUtc,
        QuantidadeParticoesArmadas = statusResultante?.Particoes.Count(p => p.Armada),
        QuantidadeParticoesDesarmadas = statusResultante?.Particoes.Count(p => !p.Armada),
        TemProblemaAtivo = statusResultante?.TemProblemaAtivo,
    };
}
