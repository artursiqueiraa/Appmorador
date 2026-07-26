using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>Pertence obrigatoriamente a um Morador. A Vaga nunca é armazenada aqui — o vínculo é uma entidade própria (<see cref="VinculoVeiculoVaga"/>), preparando vagas rotativas futuras.</summary>
public class Veiculo : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid MoradorId { get; set; }

    public Morador? Morador { get; set; }

    /// <summary>Normalizada (maiúscula, sem espaços) antes de salvar — única entre veículos não excluídos.</summary>
    public required string Placa { get; set; }

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? Cor { get; set; }

    public int? Ano { get; set; }

    public string? Observacoes { get; set; }

    public required TipoVeiculo Tipo { get; set; }

    public required StatusVeiculo Status { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
