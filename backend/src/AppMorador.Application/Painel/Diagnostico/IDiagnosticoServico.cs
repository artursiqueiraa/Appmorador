namespace AppMorador.Application.Painel.Diagnostico;

/// <summary>
/// Sprint 22B (ADR 0031) — módulo de Diagnóstico do Painel Web. Estritamente somente leitura:
/// nunca altera estado operacional/de provisionamento de nenhum equipamento (ver Fora de Escopo
/// da Sprint — ações de comando real de hardware ficam para uma Sprint futura).
/// </summary>
public interface IDiagnosticoServico
{
    Task<DiagnosticoEquipamentosPaginadosResponse> ObterStatusEquipamentosAsync(
        int pagina, int tamanhoPagina, CancellationToken cancellationToken);
}
