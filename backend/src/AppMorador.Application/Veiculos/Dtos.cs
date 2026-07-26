using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Veiculos;

public sealed class CriarVeiculoRequest
{
    [Required(ErrorMessage = "Placa é obrigatória.")]
    [MaxLength(10)]
    public string Placa { get; set; } = string.Empty;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? Cor { get; set; }

    public int? Ano { get; set; }

    public string? Observacoes { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoVeiculo Tipo { get; set; }
}

public sealed class AtualizarVeiculoRequest
{
    [Required(ErrorMessage = "Placa é obrigatória.")]
    [MaxLength(10)]
    public string Placa { get; set; } = string.Empty;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? Cor { get; set; }

    public int? Ano { get; set; }

    public string? Observacoes { get; set; }

    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoVeiculo Tipo { get; set; }

    [Required(ErrorMessage = "Status é obrigatório.")]
    public required StatusVeiculo Status { get; set; }
}

public sealed class VeiculoResponse
{
    public required Guid Id { get; init; }

    public required Guid MoradorId { get; init; }

    public required string Placa { get; init; }

    public string? Marca { get; init; }

    public string? Modelo { get; init; }

    public string? Cor { get; init; }

    public int? Ano { get; init; }

    public string? Observacoes { get; init; }

    public required TipoVeiculo Tipo { get; init; }

    public required StatusVeiculo Status { get; init; }
}
