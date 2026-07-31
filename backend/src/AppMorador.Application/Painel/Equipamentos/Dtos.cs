using AppMorador.Domain.Entities;

namespace AppMorador.Application.Painel.Equipamentos;

/// <summary>
/// Sprint 22B (ADR 0031) — DTO especializado para o Painel Web (Master/Técnico), separado do
/// `EquipamentoResponse` usado pelo Mobile em `AppMorador.Application.Equipamentos` — contratos
/// diferentes para consumidores diferentes, nunca um DTO genérico compartilhado (ver ADR 0031).
/// </summary>
public sealed class EquipamentoAdminResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public string? PropriedadeNome { get; init; }

    public required string Nome { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public string? Modelo { get; init; }

    /// <summary>Mapeado de `Equipamento.Identificador` — nome de negócio "Número de Série" (ver ADR 0031 sobre por que a coluna não foi renomeada).</summary>
    public string? NumeroSerie { get; init; }

    public required StatusEquipamento Status { get; init; }

    public required EstadoOperacionalEquipamento EstadoOperacional { get; init; }

    public string? Ip { get; init; }

    public int? Porta { get; init; }

    /// <summary>Nunca a senha — só o usuário configurado, exibível com segurança (ex.: Control iD).</summary>
    public string? Usuario { get; init; }

    public string? MacAddress { get; init; }

    public string? Observacoes { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>Sprint 22B — soft delete (ADR 0009); só aparece como `true` quando a listagem pediu `incluirRemovidos=true`.</summary>
    public required bool Excluido { get; init; }

    public DateTime? DataExclusaoUtc { get; init; }

    /// <summary>
    /// Sprint 22C.2 — só as chaves que o Provider do fabricante realmente devolveu (nunca um
    /// conjunto fixo por fabricante) — ver `Equipamento.InformacoesDescobertasJson`. Vazio/nulo
    /// quando nenhuma descoberta automática aconteceu ainda (ex.: JFL antes da central conectar
    /// pela primeira vez, ou Control iD/Intelbras que falharam ao conectar no cadastro).
    /// </summary>
    public IReadOnlyDictionary<string, string>? InformacoesDescobertas { get; init; }

    public DateTime? UltimaDescobertaUtc { get; init; }

    public DateTime? UltimaSincronizacaoUtc { get; init; }
}

public sealed class EquipamentosAdminPaginadosResponse
{
    public required IReadOnlyList<EquipamentoAdminResponse> Itens { get; init; }

    public required int PaginaAtual { get; init; }

    public required int TotalPaginas { get; init; }

    public required int TotalItens { get; init; }
}

/// <summary>
/// Sprint 22C.2 (Refatoração do Cadastro de Equipamentos) — envelope único, mas os campos
/// obrigatórios variam por <see cref="Fabricante"/> (validado em `EquipamentoAdminServico`, não
/// aqui — DTO nunca é o lugar certo pra regra condicional por valor de outro campo):
/// <list type="bullet">
/// <item>JFL: só <see cref="NumeroSerie"/> é obrigatório. Ip/Porta/Usuario/Senha são ignorados
/// mesmo se enviados — a central nunca é discada, o número de série é só a chave de
/// correlação com a sessão TCP que ela mesma abre (ver ADR 0015).</item>
/// <item>Control iD: <see cref="Ip"/>/<see cref="Porta"/>/<see cref="Usuario"/>/<see cref="Senha"/>
/// obrigatórios. <see cref="NumeroSerie"/> é ignorado — descoberto automaticamente ao salvar.</item>
/// <item>Intelbras: <see cref="Ip"/>/<see cref="Porta"/>/<see cref="Senha"/> obrigatórios (a
/// central AMT não tem conceito de usuário). <see cref="NumeroSerie"/> ignorado (sem descoberta
/// real disponível nesta integração).</item>
/// </list>
/// </summary>
public sealed class CriarEquipamentoAdminRequest
{
    public required Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public string? Modelo { get; init; }

    /// <summary>Só usado (e obrigatório) para Fabricante=Jfl — ignorado para os demais.</summary>
    public string? NumeroSerie { get; init; }

    public required EstadoOperacionalEquipamento EstadoOperacional { get; init; }

    /// <summary>Obrigatório para Control iD/Intelbras — ignorado para JFL.</summary>
    public string? Ip { get; init; }

    /// <summary>Obrigatório para Control iD/Intelbras — ignorado para JFL.</summary>
    public int? Porta { get; init; }

    /// <summary>Obrigatório só para Control iD (Intelbras/JFL não têm conceito de usuário).</summary>
    public string? Usuario { get; init; }

    /// <summary>Obrigatória para Control iD/Intelbras — nunca persistida em texto puro (ver `ICriptografiaSimetrica`). Ignorada para JFL.</summary>
    public string? Senha { get; init; }

    public string? MacAddress { get; init; }

    public string? Observacoes { get; init; }
}

/// <summary>Mesmas regras condicionais de <see cref="CriarEquipamentoAdminRequest"/>. `Senha` nula/vazia mantém a senha já cadastrada (nunca obriga redigitar).</summary>
public sealed class AtualizarEquipamentoAdminRequest
{
    public required string Nome { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public string? Modelo { get; init; }

    public string? NumeroSerie { get; init; }

    public string? Ip { get; init; }

    public int? Porta { get; init; }

    public string? Usuario { get; init; }

    /// <summary>Nulo/vazio = mantém a senha atual. Só troca quando um valor novo é enviado.</summary>
    public string? Senha { get; init; }

    public string? MacAddress { get; init; }

    public string? Observacoes { get; init; }
}

public sealed class AtualizarEstadoOperacionalRequest
{
    public required EstadoOperacionalEquipamento EstadoOperacional { get; init; }
}
