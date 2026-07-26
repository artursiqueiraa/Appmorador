using AppMorador.Application.Common;
using AppMorador.Application.Eventos;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Operacional;

/// <summary>
/// Sprint 13 — Camada Operacional Unificada (ADR 0016). Gera o Estado Bruto de cada
/// Equipamento a partir de dados já persistidos pelas integrações existentes
/// (Equipamento.Status, StatusCentralJfl) e pela Central de Eventos já existente
/// (IEventosServico) — nunca consulta IControlIdProvider/IJflProvider diretamente.
/// Fluxo obrigatório (ADR 0016): Estado Bruto → Classificador Operacional → Snapshot
/// Operacional (persistido) → Dashboard/Mobile.
/// </summary>
public sealed class SnapshotOperacionalServico : ISnapshotOperacionalServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IStatusCentralJflRepositorio _statusCentralJfl;
    private readonly ISnapshotOperacionalRepositorio _snapshots;
    private readonly IEventosServico _eventosServico;
    private readonly IClassificadorOperacionalServico _classificador;
    private readonly IOperacionalEventoPublicador _publicador;

    public SnapshotOperacionalServico(
        IPropriedadeRepositorio propriedades,
        IEquipamentoRepositorio equipamentos,
        IStatusCentralJflRepositorio statusCentralJfl,
        ISnapshotOperacionalRepositorio snapshots,
        IEventosServico eventosServico,
        IClassificadorOperacionalServico classificador,
        IOperacionalEventoPublicador publicador)
    {
        _propriedades = propriedades;
        _equipamentos = equipamentos;
        _statusCentralJfl = statusCentralJfl;
        _snapshots = snapshots;
        _eventosServico = eventosServico;
        _classificador = classificador;
        _publicador = publicador;
    }

    public async Task<Result<SnapshotOperacionalResponse>> ObterAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<SnapshotOperacionalResponse>.Fail("Propriedade não encontrada.");
        }

        var existente = await _snapshots.GetByPropriedadeIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            // O rollup numerico vem do ultimo snapshot persistido, mas a classificacao
            // por equipamento e sempre recalculada na hora — e so agregacao de dado ja
            // persistido (nunca chama Provider), custo desprezivel.
            var equipamentosAtuais = await ClassificarEquipamentosAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
            return Result<SnapshotOperacionalResponse>.Ok(ToDto(existente, equipamentosAtuais));
        }

        // Bootstrap: primeira consulta da Propriedade, nenhum snapshot ainda existe.
        // Gerar na hora e nao so devolver vazio — a geracao e so agregacao de dado ja
        // persistido (nunca chama Provider), entao nao ha custo/risco em fazer isso
        // numa leitura.
        var (snapshot, equipamentos) = await GerarEPersistirAsync(proprietarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<SnapshotOperacionalResponse>.Ok(ToDto(snapshot, equipamentos));
    }

    public async Task<Result<SnapshotOperacionalResponse>> AtualizarAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<SnapshotOperacionalResponse>.Fail("Propriedade não encontrada.");
        }

        var (snapshot, equipamentos) = await GerarEPersistirAsync(proprietarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        var dto = ToDto(snapshot, equipamentos);

        // Ação explícita do usuário (botão "Atualizar") — outros clientes conectados à
        // mesma Propriedade (ex.: outro dispositivo do mesmo dono) também são notificados.
        await _publicador.PublicarSnapshotAsync(propriedadeId, dto, MotivoAtualizacaoOperacional.SnapshotAtualizadoManualmente, cancellationToken).ConfigureAwait(false);

        return Result<SnapshotOperacionalResponse>.Ok(dto);
    }

    public async Task RegenerarEPublicarAsync(Guid propriedadeId, MotivoAtualizacaoOperacional motivo, CancellationToken cancellationToken)
    {
        // Disparado por uma mutação já concluída em Equipamento/StatusCentralJfl/Ocorrencia
        // (EquipamentoIntegracaoServico, JflComandoServico, AlarmEventProcessor) — o
        // chamador já conhece a Propriedade com segurança (resolvida via ownership check
        // ou via Central.PropriedadeId), então aqui só precisamos do dono para reaproveitar
        // IEventosServico.GetEventosAsync (que também confere posse, redundante mas barato).
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return;
        }

        var (snapshot, equipamentos) = await GerarEPersistirAsync(propriedade.ProprietarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        var dto = ToDto(snapshot, equipamentos);
        await _publicador.PublicarSnapshotAsync(propriedadeId, dto, motivo, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(SnapshotOperacional Snapshot, IReadOnlyList<EquipamentoSaudeResponse> Equipamentos)> GerarEPersistirAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var equipamentosDaPropriedade = await _equipamentos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var statusCentraisJfl = await _statusCentralJfl.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var problemaAtivoPorEquipamento = statusCentraisJfl
            .Where(s => s.TemProblemaAtivo)
            .Select(s => s.EquipamentoId)
            .ToHashSet();

        var estadosBrutos = equipamentosDaPropriedade
            .Select(e => new EstadoBrutoEquipamento
            {
                EquipamentoId = e.Id,
                Fabricante = e.Fabricante,
                Status = e.Status,
                UltimaComunicacaoUtc = e.UltimaSincronizacaoUtc,
                TemProblemaAtivo = problemaAtivoPorEquipamento.Contains(e.Id),
            })
            .ToList();

        var equipamentosClassificados = equipamentosDaPropriedade
            .Zip(estadosBrutos, (equipamento, estadoBruto) => new EquipamentoSaudeResponse
            {
                EquipamentoId = equipamento.Id,
                Nome = equipamento.Nome,
                Fabricante = equipamento.Fabricante,
                Estado = _classificador.ClassificarEquipamento(estadoBruto),
            })
            .ToList();

        var quantidadeOnline = estadosBrutos.Count(e => e.Status == StatusEquipamento.Online);
        var quantidadeOffline = estadosBrutos.Count - quantidadeOnline;
        var quantidadeFalhasDetectadas = estadosBrutos.Count(e => e.Status == StatusEquipamento.Offline);
        var quantidadeAlarmesAtivos = problemaAtivoPorEquipamento.Count;
        var ultimaComunicacaoUtc = await _equipamentos.GetUltimaSincronizacaoAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        // Reaproveita a Central de Eventos ja existente (IEventosServico) — nunca um
        // novo dominio de eventos. So o total de hoje importa aqui; tamanhoPagina=1
        // mantem a consulta barata.
        var filtroHoje = new FiltroEventos { DesdeUtc = DateTime.UtcNow.Date };
        var eventosHojeResult = await _eventosServico
            .GetEventosAsync(proprietarioId, propriedadeId, filtroHoje, pagina: 1, tamanhoPagina: 1, cancellationToken)
            .ConfigureAwait(false);
        var quantidadeEventosHoje = eventosHojeResult.Success ? eventosHojeResult.Data!.TotalItens : 0;

        var saude = _classificador.ClassificarPropriedade(estadosBrutos, quantidadeAlarmesAtivos);

        var snapshot = new SnapshotOperacional
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            GeradoEmUtc = DateTime.UtcNow,
            Saude = saude,
            QuantidadeEquipamentosOnline = quantidadeOnline,
            QuantidadeEquipamentosOffline = quantidadeOffline,
            UltimaComunicacaoUtc = ultimaComunicacaoUtc,
            QuantidadeEventosHoje = quantidadeEventosHoje,
            QuantidadeAlarmesAtivos = quantidadeAlarmesAtivos,
            QuantidadeFalhasDetectadas = quantidadeFalhasDetectadas,
        };

        await _snapshots.UpsertAsync(snapshot, cancellationToken).ConfigureAwait(false);
        await _snapshots.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (snapshot, equipamentosClassificados);
    }

    private async Task<IReadOnlyList<EquipamentoSaudeResponse>> ClassificarEquipamentosAsync(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var equipamentosDaPropriedade = await _equipamentos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var statusCentraisJfl = await _statusCentralJfl.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var problemaAtivoPorEquipamento = statusCentraisJfl
            .Where(s => s.TemProblemaAtivo)
            .Select(s => s.EquipamentoId)
            .ToHashSet();

        return equipamentosDaPropriedade
            .Select(e => new EquipamentoSaudeResponse
            {
                EquipamentoId = e.Id,
                Nome = e.Nome,
                Fabricante = e.Fabricante,
                Estado = _classificador.ClassificarEquipamento(new EstadoBrutoEquipamento
                {
                    EquipamentoId = e.Id,
                    Fabricante = e.Fabricante,
                    Status = e.Status,
                    UltimaComunicacaoUtc = e.UltimaSincronizacaoUtc,
                    TemProblemaAtivo = problemaAtivoPorEquipamento.Contains(e.Id),
                }),
            })
            .ToList();
    }

    private static SnapshotOperacionalResponse ToDto(SnapshotOperacional snapshot, IReadOnlyList<EquipamentoSaudeResponse> equipamentos) => new()
    {
        GeradoEmUtc = snapshot.GeradoEmUtc,
        Saude = snapshot.Saude,
        QuantidadeEquipamentosOnline = snapshot.QuantidadeEquipamentosOnline,
        QuantidadeEquipamentosOffline = snapshot.QuantidadeEquipamentosOffline,
        UltimaComunicacaoUtc = snapshot.UltimaComunicacaoUtc,
        QuantidadeEventosHoje = snapshot.QuantidadeEventosHoje,
        QuantidadeAlarmesAtivos = snapshot.QuantidadeAlarmesAtivos,
        QuantidadeFalhasDetectadas = snapshot.QuantidadeFalhasDetectadas,
        Equipamentos = equipamentos,
    };
}
