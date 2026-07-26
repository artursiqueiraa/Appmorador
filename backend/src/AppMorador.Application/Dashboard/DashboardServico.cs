using AppMorador.Application.Autorizacoes;
using AppMorador.Application.Common;
using AppMorador.Application.Operacional;
using AppMorador.Application.Vagas;
using AppMorador.Domain.ContactId;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Dashboard;

/// <summary>
/// Fórmula do Health Score e do texto de status/último evento — nada especulativo,
/// tudo derivado de dados reais já modelados (ver <see cref="IConsultaDashboardServico"/>).
/// </summary>
public sealed class DashboardServico : IDashboardServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUnidadeRepositorio _unidades;
    private readonly IMoradorRepositorio _moradores;
    private readonly ICredencialRepositorio _credenciais;
    private readonly IPontoAcessoRepositorio _pontosAcesso;
    private readonly IAutorizacaoRepositorio _autorizacoes;
    private readonly IVeiculoRepositorio _veiculos;
    private readonly IVagaRepositorio _vagas;
    private readonly IVinculoVeiculoVagaRepositorio _vinculosVeiculoVaga;
    private readonly IEntregaRepositorio _entregas;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IEventoEquipamentoRepositorio _eventosEquipamento;
    private readonly IStatusCentralJflRepositorio _statusCentralJfl;
    private readonly ISnapshotOperacionalServico _snapshotOperacional;
    private readonly IConsultaDashboardServico _queryService;

    public DashboardServico(
        IPropriedadeRepositorio propriedades,
        IUnidadeRepositorio unidades,
        IMoradorRepositorio moradores,
        ICredencialRepositorio credenciais,
        IPontoAcessoRepositorio pontosAcesso,
        IAutorizacaoRepositorio autorizacoes,
        IVeiculoRepositorio veiculos,
        IVagaRepositorio vagas,
        IVinculoVeiculoVagaRepositorio vinculosVeiculoVaga,
        IEntregaRepositorio entregas,
        IEquipamentoRepositorio equipamentos,
        IEventoEquipamentoRepositorio eventosEquipamento,
        IStatusCentralJflRepositorio statusCentralJfl,
        ISnapshotOperacionalServico snapshotOperacional,
        IConsultaDashboardServico queryService)
    {
        _propriedades = propriedades;
        _unidades = unidades;
        _moradores = moradores;
        _credenciais = credenciais;
        _pontosAcesso = pontosAcesso;
        _autorizacoes = autorizacoes;
        _veiculos = veiculos;
        _vagas = vagas;
        _vinculosVeiculoVaga = vinculosVeiculoVaga;
        _entregas = entregas;
        _equipamentos = equipamentos;
        _eventosEquipamento = eventosEquipamento;
        _statusCentralJfl = statusCentralJfl;
        _snapshotOperacional = snapshotOperacional;
        _queryService = queryService;
    }

    public async Task<Result<DashboardResponse>> GetAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<DashboardResponse>.Fail("Propriedade não encontrada.");
        }

        var raw = await _queryService.GetRawDataAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var quantidadeUnidades = await _unidades.CountByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var quantidadeMoradores = await _moradores.CountByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var quantidadeCredenciais = await _credenciais.CountByPropriedadeAsync(propriedadeId, status: null, cancellationToken).ConfigureAwait(false);
        var quantidadeCredenciaisAtivas = await _credenciais.CountByPropriedadeAsync(propriedadeId, StatusCredencial.Ativa, cancellationToken).ConfigureAwait(false);
        var quantidadeCredenciaisSuspensas = await _credenciais.CountByPropriedadeAsync(propriedadeId, StatusCredencial.Suspensa, cancellationToken).ConfigureAwait(false);
        var quantidadePontosAcesso = await _pontosAcesso.CountByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        // Sprint 8 — status efetivo de Autorizacao e computado (nunca gravado por um
        // job/scheduler), entao os contadores tambem sao calculados aqui a partir da
        // lista filtrada por propriedade — mesma regra unica de AutorizacaoServico
        // (StatusAutorizacaoCalculator), nunca duplicada.
        var agora = DateTime.UtcNow;
        var autorizacoesDaPropriedade = await _autorizacoes.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var statusPorAutorizacao = autorizacoesDaPropriedade
            .Select(a => (Autorizacao: a, Status: StatusAutorizacaoCalculator.CalcularEfetivo(a, agora)))
            .ToList();
        var quantidadeVisitantesAtivos = statusPorAutorizacao
            .Where(x => x.Status == StatusAutorizacao.Ativa)
            .Select(x => x.Autorizacao.VisitanteId)
            .Distinct()
            .Count();
        var quantidadeAutorizacoesPendentes = statusPorAutorizacao.Count(x => x.Status == StatusAutorizacao.Pendente);
        var quantidadeAutorizacoesExpiradas = statusPorAutorizacao.Count(x => x.Status == StatusAutorizacao.Expirada);

        var quantidadeVeiculos = await _veiculos.CountByPropriedadeAsync(propriedadeId, status: null, cancellationToken).ConfigureAwait(false);
        var quantidadeVeiculosAtivos = await _veiculos.CountByPropriedadeAsync(propriedadeId, StatusVeiculo.Ativo, cancellationToken).ConfigureAwait(false);
        var quantidadeVagas = await _vagas.CountByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        // Sprint 9 — mesma logica hibrida das Autorizacoes: Status efetivo da Vaga e
        // computado (nunca gravado por um job), entao os contadores tambem sao
        // calculados aqui a partir da lista filtrada por propriedade — mesma regra
        // unica de VagaServico (VagaStatusCalculator), nunca duplicada.
        var vagasDaPropriedade = await _vagas.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var vinculosAtivosDaPropriedade = await _vinculosVeiculoVaga.ListAtivosByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var vagasComVinculoAtivo = vinculosAtivosDaPropriedade.Select(v => v.VagaId).ToHashSet();
        var statusPorVaga = vagasDaPropriedade
            .Select(v => VagaStatusCalculator.CalcularEfetivo(v, vagasComVinculoAtivo.Contains(v.Id)))
            .ToList();
        var quantidadeVagasLivres = statusPorVaga.Count(s => s == StatusVaga.Livre);
        var quantidadeVagasOcupadas = statusPorVaga.Count(s => s == StatusVaga.Ocupada);

        // Sprint 10 — Status de Entrega e 100% manual (sem calculadora hibrida, ver
        // ADR 0013) — contagens diretas por status, sem necessidade de computar nada.
        var quantidadeEntregasPendentes = await _entregas.CountByPropriedadeAsync(propriedadeId, StatusEntrega.AguardandoRecebimento, cancellationToken).ConfigureAwait(false);
        var quantidadeEntregasDisponiveis = await _entregas.CountByPropriedadeAsync(propriedadeId, StatusEntrega.DisponivelParaRetirada, cancellationToken).ConfigureAwait(false);
        var quantidadeEntregasRetiradas = await _entregas.CountByPropriedadeAsync(propriedadeId, StatusEntrega.Retirada, cancellationToken).ConfigureAwait(false);
        var quantidadeCorrespondenciasCadastradas = await _entregas.CountByPropriedadeAsync(propriedadeId, status: null, cancellationToken).ConfigureAwait(false);

        // Sprint 13 — Camada Operacional Unificada (ADR 0016): Equipamentos Online/
        // Offline vem exclusivamente do Snapshot Operacional (que ja agrega o mesmo
        // dado persistido — Equipamento.Status — nunca um Provider), evitando
        // recalcular a mesma coisa duas vezes. Snapshot e gerado sob demanda (nunca
        // por polling) na primeira consulta da Propriedade, ou reaproveitado se ja
        // existir.
        var snapshotResult = await _snapshotOperacional.ObterAsync(proprietarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        var snapshot = snapshotResult.Data;
        var quantidadeEquipamentosOnline = snapshot?.QuantidadeEquipamentosOnline ?? 0;
        var quantidadeEquipamentosOffline = snapshot?.QuantidadeEquipamentosOffline ?? 0;
        var ultimaSincronizacaoUtc = await _equipamentos.GetUltimaSincronizacaoAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var ultimoEventoEquipamentoRecebidoUtc = await _eventosEquipamento.GetUltimoRecebidoAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        // Sprint 12 — Migracao JFL Active 100 Bus (ADR 0015). Online/Offline reaproveita
        // o mesmo Equipamento.Status generico (Sprint 11); Particoes/Problemas vem do
        // rollup StatusCentralJfl, atualizado so por acao explicita do usuario (nunca
        // por polling — mesma regra de UltimaSincronizacaoUtc acima).
        var equipamentosDaPropriedade = await _equipamentos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var centraisJfl = equipamentosDaPropriedade.Where(e => e.Fabricante == FabricanteEquipamento.Jfl).ToList();
        var quantidadeCentraisJflOnline = centraisJfl.Count(e => e.Status == StatusEquipamento.Online);
        var quantidadeCentraisJflOffline = centraisJfl.Count - quantidadeCentraisJflOnline;

        var statusCentraisJflDaPropriedade = await _statusCentralJfl.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var quantidadeParticoesArmadas = statusCentraisJflDaPropriedade.Sum(s => s.QuantidadeParticoesArmadas);
        var quantidadeParticoesDesarmadas = statusCentraisJflDaPropriedade.Sum(s => s.QuantidadeParticoesDesarmadas);
        var quantidadeProblemasAtivosJfl = statusCentraisJflDaPropriedade.Count(s => s.TemProblemaAtivo);

        var statusSeguranca = raw.TemCentral ? "Protegido" : "Configuração pendente";

        var cobertura = raw.TotalZonas > 0 ? (double)raw.ZonasComCamera / raw.TotalZonas : 0d;
        var pontuacaoSaude = (raw.TemCentral ? 50 : 0) + (int)Math.Round(50 * cobertura);

        string? ultimoEvento = null;
        if (raw.UltimaOcorrenciaCodigoContactId is not null &&
            ContactIdCatalog.TryGet(raw.UltimaOcorrenciaCodigoContactId, out var definicao))
        {
            var local = raw.UltimaOcorrenciaNomeZona ?? "um local monitorado";
            ultimoEvento = $"{definicao!.FriendlyMessage} em {local}";
        }

        var dto = new DashboardResponse
        {
            Nome = propriedade.Nome,
            Tipo = propriedade.Tipo,
            StatusSeguranca = statusSeguranca,
            PontuacaoSaude = pontuacaoSaude,
            UltimoEvento = ultimoEvento,
            UltimoEventoEmUtc = raw.UltimaOcorrenciaEmUtc,
            QuantidadeCentrais = raw.QuantidadeCentrais,
            QuantidadeGravadores = raw.QuantidadeGravadores,
            QuantidadeCameras = raw.QuantidadeCameras,
            QuantidadeSensores = raw.TotalZonas,
            QuantidadeUnidades = quantidadeUnidades,
            QuantidadePessoas = quantidadeMoradores, // Sprint 6 — antes era sempre 1 (so o dono)
            QuantidadeCredenciais = quantidadeCredenciais,
            QuantidadeCredenciaisAtivas = quantidadeCredenciaisAtivas,
            QuantidadeCredenciaisSuspensas = quantidadeCredenciaisSuspensas,
            QuantidadePontosAcesso = quantidadePontosAcesso,
            QuantidadeVisitantesAtivos = quantidadeVisitantesAtivos,
            QuantidadeAutorizacoesPendentes = quantidadeAutorizacoesPendentes,
            QuantidadeAutorizacoesExpiradas = quantidadeAutorizacoesExpiradas,
            QuantidadeVeiculos = quantidadeVeiculos,
            QuantidadeVeiculosAtivos = quantidadeVeiculosAtivos,
            QuantidadeVagas = quantidadeVagas,
            QuantidadeVagasLivres = quantidadeVagasLivres,
            QuantidadeVagasOcupadas = quantidadeVagasOcupadas,
            QuantidadeEntregasPendentes = quantidadeEntregasPendentes,
            QuantidadeEntregasDisponiveis = quantidadeEntregasDisponiveis,
            QuantidadeEntregasRetiradas = quantidadeEntregasRetiradas,
            QuantidadeCorrespondenciasCadastradas = quantidadeCorrespondenciasCadastradas,
            QuantidadeEquipamentosOnline = quantidadeEquipamentosOnline,
            QuantidadeEquipamentosOffline = quantidadeEquipamentosOffline,
            UltimaSincronizacaoUtc = ultimaSincronizacaoUtc,
            UltimoEventoEquipamentoRecebidoUtc = ultimoEventoEquipamentoRecebidoUtc,
            QuantidadeCentraisJflOnline = quantidadeCentraisJflOnline,
            QuantidadeCentraisJflOffline = quantidadeCentraisJflOffline,
            QuantidadeParticoesArmadas = quantidadeParticoesArmadas,
            QuantidadeParticoesDesarmadas = quantidadeParticoesDesarmadas,
            QuantidadeProblemasAtivosJfl = quantidadeProblemasAtivosJfl,
            Saude = snapshot?.Saude ?? EstadoOperacional.Saudavel,
            QuantidadeEventosHoje = snapshot?.QuantidadeEventosHoje ?? 0,
            QuantidadeAlarmesAtivos = snapshot?.QuantidadeAlarmesAtivos ?? 0,
            UltimaAtualizacaoOperacionalUtc = snapshot?.GeradoEmUtc,
        };

        return Result<DashboardResponse>.Ok(dto);
    }
}
