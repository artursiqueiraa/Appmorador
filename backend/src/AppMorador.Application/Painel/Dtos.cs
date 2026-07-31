namespace AppMorador.Application.Painel;

/// <summary>Sprint 22A (ADR 0029) — "cliente" na linguagem do Painel Web é o Usuario dono de propriedade (RoleGlobal nulo).</summary>
public sealed class ProprietarioResponse
{
    public required Guid Id { get; init; }

    public required string Nome { get; init; }

    public required string Email { get; init; }

    public required bool Ativo { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required int QuantidadePropriedades { get; init; }
}

/// <summary>Sprint 22A — resumo de propriedade para a tela de detalhe do cliente e para escolher em qual impersonar.</summary>
public sealed class PropriedadeResumoResponse
{
    public required Guid Id { get; init; }

    public required string Nome { get; init; }

    public required string Tipo { get; init; }
}

public sealed class ProprietarioDetalheResponse
{
    public required Guid Id { get; init; }

    public required string Nome { get; init; }

    public required string Email { get; init; }

    public required bool Ativo { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required IReadOnlyList<PropriedadeResumoResponse> Propriedades { get; init; }
}

public sealed class ProprietariosPaginadosResponse
{
    public required IReadOnlyList<ProprietarioResponse> Itens { get; init; }

    public required int PaginaAtual { get; init; }

    public required int TotalPaginas { get; init; }

    public required int TotalItens { get; init; }
}

public sealed class NovosClientesPorMesItem
{
    /// <summary>Formato "AAAA-MM".</summary>
    public required string Mes { get; init; }

    public required int Quantidade { get; init; }
}

/// <summary>
/// Sprint 22A (ADR 0029) — agregado cross-propriedade para o Dashboard Operacional do Painel
/// Web. Não existia nenhuma consulta assim antes desta Sprint (tudo no domínio é escopado por
/// dono) — achado da Fase 0, ver docs/painel/mapeamento-api.md. "Propriedades por Tipo" substitui
/// o "Propriedades por Status" pedido pela missão original: Propriedade não tem nenhum campo de
/// status (Ativo/Inativo/Pendente) — só soft delete — então o breakdown real disponível é por
/// TipoPropriedade.
/// </summary>
public sealed class DashboardOperacionalResponse
{
    public required int TotalClientes { get; init; }

    public required int TotalPropriedades { get; init; }

    public required int TotalEquipamentos { get; init; }

    public required int TotalEquipamentosOffline { get; init; }

    public required IReadOnlyList<NovosClientesPorMesItem> NovosClientesPorMes { get; init; }

    public required IReadOnlyDictionary<string, int> PropriedadesPorTipo { get; init; }

    public required IReadOnlyDictionary<string, int> EquipamentosPorStatus { get; init; }
}
