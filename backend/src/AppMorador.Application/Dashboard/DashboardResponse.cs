using AppMorador.Domain.Entities;

namespace AppMorador.Application.Dashboard;

/// <summary>
/// Contrato do dashboard exposto ao app. Nunca contem termos tecnicos (Contact ID,
/// numero de zona, DVR, ISAPI, ONVIF) — so linguagem que o usuario final entende.
/// </summary>
public sealed class DashboardResponse
{
    public required string Nome { get; init; }

    public required TipoPropriedade Tipo { get; init; }

    public required string StatusSeguranca { get; init; }

    public required int PontuacaoSaude { get; init; }

    public string? UltimoEvento { get; init; }

    public DateTime? UltimoEventoEmUtc { get; init; }

    public required int QuantidadeCentrais { get; init; }

    public required int QuantidadeGravadores { get; init; }

    public required int QuantidadeCameras { get; init; }

    public required int QuantidadeSensores { get; init; }

    /// <summary>Sprint 6 — total de Unidades ativas (não excluídas) da propriedade.</summary>
    public required int QuantidadeUnidades { get; init; }

    /// <summary>Sprint 6 — total de Moradores ativos (não excluídos) da propriedade. Antes desta Sprint era sempre 1 (só o dono).</summary>
    public required int QuantidadePessoas { get; init; }

    /// <summary>Sprint 7 — total de Credenciais ativas (não excluídas) da propriedade, em qualquer status.</summary>
    public required int QuantidadeCredenciais { get; init; }

    /// <summary>Sprint 7 — Credenciais com Status = Ativa.</summary>
    public required int QuantidadeCredenciaisAtivas { get; init; }

    /// <summary>Sprint 7 — Credenciais com Status = Suspensa.</summary>
    public required int QuantidadeCredenciaisSuspensas { get; init; }

    /// <summary>Sprint 7 — total de Pontos de Acesso cadastrados na propriedade.</summary>
    public required int QuantidadePontosAcesso { get; init; }

    /// <summary>Sprint 8 — Visitantes distintos com ao menos uma Autorizacao com status efetivo Ativa agora.</summary>
    public required int QuantidadeVisitantesAtivos { get; init; }

    /// <summary>Sprint 8 — Autorizacoes com status efetivo Pendente agora.</summary>
    public required int QuantidadeAutorizacoesPendentes { get; init; }

    /// <summary>Sprint 8 — Autorizacoes com status efetivo Expirada agora.</summary>
    public required int QuantidadeAutorizacoesExpiradas { get; init; }

    /// <summary>Sprint 9 — total de Veiculos ativos (não excluídos) da propriedade, em qualquer status.</summary>
    public required int QuantidadeVeiculos { get; init; }

    /// <summary>Sprint 9 — Veiculos com Status = Ativo.</summary>
    public required int QuantidadeVeiculosAtivos { get; init; }

    /// <summary>Sprint 9 — total de Vagas cadastradas na propriedade.</summary>
    public required int QuantidadeVagas { get; init; }

    /// <summary>Sprint 9 — Vagas com status efetivo Livre agora.</summary>
    public required int QuantidadeVagasLivres { get; init; }

    /// <summary>Sprint 9 — Vagas com status efetivo Ocupada agora.</summary>
    public required int QuantidadeVagasOcupadas { get; init; }

    /// <summary>Sprint 10 — Entregas com Status = AguardandoRecebimento.</summary>
    public required int QuantidadeEntregasPendentes { get; init; }

    /// <summary>Sprint 10 — Entregas com Status = DisponivelParaRetirada.</summary>
    public required int QuantidadeEntregasDisponiveis { get; init; }

    /// <summary>Sprint 10 — Entregas com Status = Retirada.</summary>
    public required int QuantidadeEntregasRetiradas { get; init; }

    /// <summary>Sprint 10 — total de Entregas cadastradas (não excluídas) na propriedade, em qualquer tipo/status.</summary>
    public required int QuantidadeCorrespondenciasCadastradas { get; init; }

    /// <summary>Sprint 11 — Equipamentos com Status = Online (última comunicação bem-sucedida).</summary>
    public required int QuantidadeEquipamentosOnline { get; init; }

    /// <summary>Sprint 11 — Equipamentos com Status = Offline ou Desconhecido (nunca comunicou ou última tentativa falhou).</summary>
    public required int QuantidadeEquipamentosOffline { get; init; }

    /// <summary>Sprint 11 — sincronização manual (Moradores/Credenciais/Permissões) mais recente entre todos os Equipamentos da propriedade.</summary>
    public DateTime? UltimaSincronizacaoUtc { get; init; }

    /// <summary>Sprint 11 — evento de equipamento importado mais recente entre todos os Equipamentos da propriedade.</summary>
    public DateTime? UltimoEventoEquipamentoRecebidoUtc { get; init; }

    /// <summary>Sprint 12 — Centrais JFL (Equipamento Fabricante=Jfl) com Status = Online.</summary>
    public required int QuantidadeCentraisJflOnline { get; init; }

    /// <summary>Sprint 12 — Centrais JFL com Status = Offline ou Desconhecido.</summary>
    public required int QuantidadeCentraisJflOffline { get; init; }

    /// <summary>Sprint 12 — total de partições armadas (ou armadas Stay) no último status conhecido de todas as centrais JFL da propriedade.</summary>
    public required int QuantidadeParticoesArmadas { get; init; }

    /// <summary>Sprint 12 — total de partições desarmadas no último status conhecido de todas as centrais JFL da propriedade.</summary>
    public required int QuantidadeParticoesDesarmadas { get; init; }

    /// <summary>Sprint 12 — quantidade de centrais JFL com ao menos um problema ativo no último status conhecido.</summary>
    public required int QuantidadeProblemasAtivosJfl { get; init; }

    /// <summary>Sprint 13 — Camada Operacional Unificada (ADR 0016). Classificação consolidada (Saudável/Atenção/Crítico/Offline) de toda a Propriedade, vinda exclusivamente do Snapshot Operacional.</summary>
    public required EstadoOperacional Saude { get; init; }

    /// <summary>Sprint 13 — total de eventos (todas as origens) ocorridos desde a meia-noite UTC de hoje.</summary>
    public required int QuantidadeEventosHoje { get; init; }

    /// <summary>Sprint 13 — total de alarmes/problemas ativos consolidados entre todos os equipamentos da propriedade (qualquer fabricante).</summary>
    public required int QuantidadeAlarmesAtivos { get; init; }

    /// <summary>Sprint 13 — quando o Snapshot Operacional foi gerado por último (consulta ou atualização manual).</summary>
    public DateTime? UltimaAtualizacaoOperacionalUtc { get; init; }
}
