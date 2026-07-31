using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>
/// Sprint 22B (ADR 0031) — leitura agregada cross-aggregate (Equipamento + StatusCentralJfl +
/// EventoEquipamento) para o módulo de Diagnóstico do Painel Web. Estritamente somente leitura:
/// nunca altera estado operacional/de provisionamento. Projeta tudo em uma única consulta (ver
/// implementação EF Core) para evitar N+1 — nenhum outro Servico/Controller deve iterar
/// equipamentos e consultar StatusCentralJfl/EventoEquipamento um a um.
/// </summary>
public interface IDiagnosticoEquipamentoRepositorio
{
    Task<(IReadOnlyList<DiagnosticoEquipamentoDados> Itens, int Total)> ListarStatusAsync(
        int pagina, int tamanhoPagina, CancellationToken cancellationToken);
}

/// <summary>Projeção de leitura (não é entidade persistida) — combina dados de 3 agregados para o Diagnóstico.</summary>
public sealed class DiagnosticoEquipamentoDados
{
    public required Guid EquipamentoId { get; init; }

    public required string EquipamentoNome { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string PropriedadeNome { get; init; }

    public required StatusEquipamento Status { get; init; }

    public required EstadoOperacionalEquipamento EstadoOperacional { get; init; }

    public DateTime? UltimaSincronizacaoUtc { get; init; }

    /// <summary>Só populado para centrais JFL (ver StatusCentralJfl, 1:1 com Equipamento).</summary>
    public DateTime? StatusCentralCapturadoEmUtc { get; init; }

    public bool? StatusCentralTemProblemaAtivo { get; init; }

    /// <summary>Quantidade de EventoEquipamento nos últimos 7 dias.</summary>
    public required int QuantidadeEventosRecentes { get; init; }

    public string? UltimoEventoDescricao { get; init; }

    public DateTime? UltimoEventoEmUtc { get; init; }
}
