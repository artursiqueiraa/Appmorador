using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Equipamentos;

/// <summary>
/// Ip/Porta/Usuario/Senha são opcionais no DTO porque nem todo fabricante os usa —
/// Control iD disca para o equipamento (todos obrigatórios); JFL é o oposto, a
/// central disca para o AppMorador, então não há para onde discar (só
/// <see cref="Identificador"/>, o número de série, importa). A obrigatoriedade real
/// por fabricante é validada em <c>EquipamentoServico</c>, não aqui (ver ADR 0015).
/// </summary>
public sealed class CriarEquipamentoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public required string Nome { get; set; }

    public string? Modelo { get; set; }

    [Required(ErrorMessage = "Fabricante é obrigatório.")]
    public required FabricanteEquipamento Fabricante { get; set; }

    public string? Ip { get; set; }

    [Range(1, 65535, ErrorMessage = "Porta deve estar entre 1 e 65535.")]
    public int? Porta { get; set; }

    public string? Usuario { get; set; }

    public string? Senha { get; set; }

    public string? Identificador { get; set; }
}

/// <summary>Senha é opcional na edição — em branco preserva a senha já cadastrada (nunca é devolvida para o cliente re-enviar).</summary>
public sealed class AtualizarEquipamentoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    public required string Nome { get; set; }

    public string? Modelo { get; set; }

    [Required(ErrorMessage = "Fabricante é obrigatório.")]
    public required FabricanteEquipamento Fabricante { get; set; }

    public string? Ip { get; set; }

    [Range(1, 65535, ErrorMessage = "Porta deve estar entre 1 e 65535.")]
    public int? Porta { get; set; }

    public string? Usuario { get; set; }

    public string? Senha { get; set; }

    public string? Identificador { get; set; }
}

/// <summary>Nunca inclui a senha (nem cifrada) — só um sinal de que existe uma configurada, ver ADR 0014.</summary>
public sealed class EquipamentoResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public string? Modelo { get; init; }

    /// <summary>Sprint 21 (ADR 0027) — id do catálogo, para quem quiser consultar capacidades via GET /api/equipamentos/{id}/capacidades.</summary>
    public Guid? ModeloEquipamentoId { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public string? Ip { get; init; }

    public int? Porta { get; init; }

    public string? Usuario { get; init; }

    public string? Identificador { get; init; }

    public required StatusEquipamento Status { get; init; }

    public DateTime? UltimaSincronizacaoUtc { get; init; }
}
