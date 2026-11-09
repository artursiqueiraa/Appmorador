using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Painel.NovasInstalacoes;

/// <summary>
/// Akteck Seg (ADR 0035) — tudo o que uma instalação nova precisa num envio só:
/// a conta do cliente, a primeira propriedade e, opcionalmente, um equipamento já
/// cadastrado e ainda livre para ser vinculado.
/// </summary>
public sealed class CriarNovaInstalacaoRequest
{
    [Required(ErrorMessage = "Nome do cliente é obrigatório.")]
    [MaxLength(200)]
    public string NomeCliente { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail do cliente é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(256)]
    public string EmailCliente { get; set; } = string.Empty;

    /// <summary>Senha inicial — mesma regra de <c>CadastrarUsuarioRequest</c>.</summary>
    [Required(ErrorMessage = "Senha inicial é obrigatória.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "A senha deve ter ao menos 8 caracteres, com letra maiúscula, minúscula e número.")]
    public string SenhaInicial { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nome da propriedade é obrigatório.")]
    [MaxLength(200)]
    public string NomePropriedade { get; set; } = string.Empty;

    [EnumDataType(typeof(TipoPropriedade), ErrorMessage = "Tipo de propriedade inválido.")]
    public required TipoPropriedade TipoPropriedade { get; set; }

    [MaxLength(300)]
    public string? EnderecoPropriedade { get; set; }

    /// <summary>Equipamento a vincular na propriedade nova. Nulo = instalação sem equipamento por enquanto.</summary>
    public Guid? EquipamentoId { get; set; }
}

public sealed class NovaInstalacaoResponse
{
    public required Guid ClienteId { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string NomePropriedade { get; init; }

    public Guid? VinculoEquipamentoId { get; init; }
}
