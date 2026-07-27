using AppMorador.Application.Common;
using AppMorador.Application.Notificacoes;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Jfl;

/// <summary>
/// Orquestra a comunicação real com uma central JFL: resolve o Equipamento
/// (Fabricante=Jfl) e seu Número de Série (<see cref="Equipamento.Identificador"/>),
/// delega a <see cref="IJflProvider"/>, e persiste o rollup de status
/// (<see cref="StatusCentralJfl"/>) usado pelo Dashboard após qualquer ação
/// bem-sucedida. Nunca conhece o protocolo JFL por dentro — isso é exclusividade do
/// Provider (ADR 0014/0015).
/// </summary>
public sealed class JflComandoServico : IJflComandoServico
{
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly ICentralRepositorio _centrais;
    private readonly IStatusCentralJflRepositorio _statusCentralJfl;
    private readonly IJflProvider _jflProvider;
    private readonly ISnapshotOperacionalServico _snapshotOperacional;
    private readonly INotificationDispatcher _notificationDispatcher;

    public JflComandoServico(
        IEquipamentoRepositorio equipamentos,
        ICentralRepositorio centrais,
        IStatusCentralJflRepositorio statusCentralJfl,
        IJflProvider jflProvider,
        ISnapshotOperacionalServico snapshotOperacional,
        INotificationDispatcher notificationDispatcher)
    {
        _equipamentos = equipamentos;
        _centrais = centrais;
        _statusCentralJfl = statusCentralJfl;
        _jflProvider = jflProvider;
        _snapshotOperacional = snapshotOperacional;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<CentralJflResponse>> ObterDetalhesAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoJflAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<CentralJflResponse>.Fail("Central JFL não encontrada.");
        }

        var central = await _centrais
            .GetByPropriedadeIdENumeroSerieAsync(equipamento.PropriedadeId, equipamento.Identificador!, cancellationToken)
            .ConfigureAwait(false);

        var snapshot = await _statusCentralJfl.GetByEquipamentoIdAsync(equipamento.Id, cancellationToken).ConfigureAwait(false);

        return Result<CentralJflResponse>.Ok(ToDto(equipamento, central, snapshot));
    }

    public async Task<Result<ResultadoTesteConexaoJfl>> TestarConexaoAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoJflAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<ResultadoTesteConexaoJfl>.Fail("Central JFL não encontrada.");
        }

        var resultado = await _jflProvider.TestarConexaoAsync(equipamento.Identificador!, cancellationToken).ConfigureAwait(false);

        var statusAnterior = equipamento.Status;
        equipamento.Status = resultado.Sucesso ? StatusEquipamento.Online : StatusEquipamento.Offline;
        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
        await NotificarTransicaoOfflineAsync(equipamento, statusAnterior, cancellationToken).ConfigureAwait(false);

        return Result<ResultadoTesteConexaoJfl>.Ok(resultado);
    }

    public Task<Result<ResultadoComandoJfl>> ConsultarStatusAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (numeroSerie, ct) => _jflProvider.ConsultarStatusAsync(numeroSerie, ct), notificarEmSucesso: null, cancellationToken);

    public Task<Result<ResultadoComandoJfl>> ArmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (numeroSerie, ct) => _jflProvider.ArmarAsync(numeroSerie, particao, ct), EventoNotificacaoTipo.SistemaArmado, cancellationToken);

    public Task<Result<ResultadoComandoJfl>> DesarmarAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (numeroSerie, ct) => _jflProvider.DesarmarAsync(numeroSerie, particao, ct), EventoNotificacaoTipo.SistemaDesarmado, cancellationToken);

    public Task<Result<ResultadoComandoJfl>> ArmarStayAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (numeroSerie, ct) => _jflProvider.ArmarStayAsync(numeroSerie, particao, ct), EventoNotificacaoTipo.SistemaArmado, cancellationToken);

    public Task<Result<ResultadoComandoJfl>> ArmarAwayAsync(Guid proprietarioId, Guid equipamentoId, int particao, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (numeroSerie, ct) => _jflProvider.ArmarAwayAsync(numeroSerie, particao, ct), EventoNotificacaoTipo.SistemaArmado, cancellationToken);

    // Sprint 19 — só AcionarPgm (ligar) notifica: é o análogo ao "portão aberto" da
    // missão (uma ação que acabou de acontecer e é relevante saber). DesligarPgm não
    // está na tabela de eventos da Fase 4 e normalmente é o próprio morador desfazendo
    // a própria ação segundos depois — notificar geraria ruído sem trazer novidade.
    public Task<Result<ResultadoComandoJfl>> AcionarPgmAsync(Guid proprietarioId, Guid equipamentoId, int pgmNumero, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (numeroSerie, ct) => _jflProvider.AcionarPgmAsync(numeroSerie, pgmNumero, ct), EventoNotificacaoTipo.ComandoAcionado, cancellationToken);

    public Task<Result<ResultadoComandoJfl>> DesligarPgmAsync(Guid proprietarioId, Guid equipamentoId, int pgmNumero, CancellationToken cancellationToken) =>
        ExecutarComandoAsync(proprietarioId, equipamentoId, (numeroSerie, ct) => _jflProvider.DesligarPgmAsync(numeroSerie, pgmNumero, ct), notificarEmSucesso: null, cancellationToken);

    public async Task<Result<ResultadoComandoJfl>> InibirZonaAsync(Guid proprietarioId, Guid equipamentoId, int zonaNumero, CancellationToken cancellationToken)
    {
        return await AtualizarInibicaoAsync(proprietarioId, equipamentoId, zonaNumero, inibir: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<ResultadoComandoJfl>> DesinibirZonaAsync(Guid proprietarioId, Guid equipamentoId, int zonaNumero, CancellationToken cancellationToken)
    {
        return await AtualizarInibicaoAsync(proprietarioId, equipamentoId, zonaNumero, inibir: false, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<ResultadoComandoJfl>> AtualizarInibicaoAsync(
        Guid proprietarioId, Guid equipamentoId, int zonaNumero, bool inibir, CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoJflAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<ResultadoComandoJfl>.Fail("Central JFL não encontrada.");
        }

        var numeroSerie = equipamento.Identificador!;

        // O comando 0x52 substitui o conjunto inteiro de zonas inibidas — por isso
        // primeiro consultamos o status atual para saber quais já estão inibidas.
        var statusAtual = await _jflProvider.ConsultarStatusAsync(numeroSerie, cancellationToken).ConfigureAwait(false);
        if (!statusAtual.Sucesso || statusAtual.StatusResultante is null)
        {
            return Result<ResultadoComandoJfl>.Ok(statusAtual);
        }

        var zonasInibidas = statusAtual.StatusResultante.Zonas
            .Where(z => z.Estado == "Inibida")
            .Select(z => z.Numero)
            .ToHashSet();

        if (inibir)
        {
            zonasInibidas.Add(zonaNumero);
        }
        else
        {
            zonasInibidas.Remove(zonaNumero);
        }

        var resultado = await _jflProvider.InibirZonasAsync(numeroSerie, zonasInibidas, cancellationToken).ConfigureAwait(false);
        await AtualizarEquipamentoAposComandoAsync(equipamento, resultado, cancellationToken).ConfigureAwait(false);

        return Result<ResultadoComandoJfl>.Ok(resultado);
    }

    private async Task<Result<ResultadoComandoJfl>> ExecutarComandoAsync(
        Guid proprietarioId, Guid equipamentoId, Func<string, CancellationToken, Task<ResultadoComandoJfl>> executar,
        EventoNotificacaoTipo? notificarEmSucesso, CancellationToken cancellationToken)
    {
        var equipamento = await ResolverEquipamentoJflAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<ResultadoComandoJfl>.Fail("Central JFL não encontrada.");
        }

        var resultado = await executar(equipamento.Identificador!, cancellationToken).ConfigureAwait(false);
        await AtualizarEquipamentoAposComandoAsync(equipamento, resultado, cancellationToken).ConfigureAwait(false);

        if (notificarEmSucesso is not null && resultado.Sucesso)
        {
            await _notificationDispatcher.NotificarAsync(notificarEmSucesso.Value, new ContextoNotificacao
            {
                PropriedadeId = equipamento.PropriedadeId,
                NomePropriedade = equipamento.Propriedade?.Nome ?? "sua propriedade",
            }, cancellationToken).ConfigureAwait(false);
        }

        return Result<ResultadoComandoJfl>.Ok(resultado);
    }

    private async Task AtualizarEquipamentoAposComandoAsync(Equipamento equipamento, ResultadoComandoJfl resultado, CancellationToken cancellationToken)
    {
        var statusAnterior = equipamento.Status;
        equipamento.Status = resultado.Sucesso ? StatusEquipamento.Online : StatusEquipamento.Offline;

        if (resultado.Sucesso && resultado.StatusResultante is not null)
        {
            equipamento.UltimaSincronizacaoUtc = DateTime.UtcNow;

            await _statusCentralJfl.UpsertAsync(new StatusCentralJfl
            {
                Id = Guid.NewGuid(),
                EquipamentoId = equipamento.Id,
                CapturadoEmUtc = DateTime.UtcNow,
                QuantidadeParticoesArmadas = resultado.StatusResultante.Particoes.Count(p => p.Armada || p.ArmadaStay),
                QuantidadeParticoesDesarmadas = resultado.StatusResultante.Particoes.Count(p => !p.Desabilitada && !p.Armada && !p.ArmadaStay),
                TemProblemaAtivo = resultado.StatusResultante.ProblemasAtivos.Count > 0,
            }, cancellationToken).ConfigureAwait(false);
        }

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _statusCentralJfl.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
        await NotificarTransicaoOfflineAsync(equipamento, statusAnterior, cancellationToken).ConfigureAwait(false);
    }

    // Sprint 19 (Fase 4) — só a transição Online/Desconhecido → Offline notifica; a
    // missão é explícita que "Equipamento online" nunca notifica (não está nem no
    // enum EventoNotificacaoTipo). Sem isso, toda sincronização bem-sucedida
    // repetiria "offline" mesmo quando o equipamento já estava offline antes.
    private Task NotificarTransicaoOfflineAsync(Equipamento equipamento, StatusEquipamento statusAnterior, CancellationToken cancellationToken)
    {
        if (statusAnterior == StatusEquipamento.Offline || equipamento.Status != StatusEquipamento.Offline)
        {
            return Task.CompletedTask;
        }

        return _notificationDispatcher.NotificarAsync(EventoNotificacaoTipo.EquipamentoOffline, new ContextoNotificacao
        {
            PropriedadeId = equipamento.PropriedadeId,
            NomePropriedade = equipamento.Propriedade?.Nome ?? "sua propriedade",
            EquipamentoId = equipamento.Id,
            NomeEquipamento = equipamento.Nome,
        }, cancellationToken);
    }

    // Sprint 14 (ADR 0017) — cobre os dois pontos que mudam Equipamento.Status/StatusCentralJfl
    // fora de TestarConexaoAsync: ExecutarComandoAsync e AtualizarInibicaoAsync, ambos via
    // AtualizarEquipamentoAposComandoAsync. Nunca lança — falha de publicação em tempo real
    // não pode transformar um comando já bem-sucedido em erro para o usuário.
    private Task PublicarAtualizacaoAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _snapshotOperacional.RegenerarEPublicarAsync(propriedadeId, MotivoAtualizacaoOperacional.EquipamentoStatusAlterado, cancellationToken);

    private async Task<Equipamento?> ResolverEquipamentoJflAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento?.Propriedade is null || equipamento.Propriedade.ProprietarioId != proprietarioId)
        {
            return null;
        }

        return equipamento.Fabricante == FabricanteEquipamento.Jfl && !string.IsNullOrWhiteSpace(equipamento.Identificador)
            ? equipamento
            : null;
    }

    private static CentralJflResponse ToDto(Equipamento equipamento, Central? central, StatusCentralJfl? snapshot) => new()
    {
        EquipamentoId = equipamento.Id,
        PropriedadeId = equipamento.PropriedadeId,
        Nome = equipamento.Nome,
        Modelo = equipamento.ModeloEquipamento?.Nome,
        NumeroSerie = equipamento.Identificador!,
        Status = equipamento.Status,
        UltimaSincronizacaoUtc = equipamento.UltimaSincronizacaoUtc,
        CentralVinculadaId = central?.Id,
        CentralVinculadaNome = central?.Nome,
        QuantidadeParticoesArmadas = snapshot?.QuantidadeParticoesArmadas,
        QuantidadeParticoesDesarmadas = snapshot?.QuantidadeParticoesDesarmadas,
        TemProblemaAtivo = snapshot?.TemProblemaAtivo,
    };
}
