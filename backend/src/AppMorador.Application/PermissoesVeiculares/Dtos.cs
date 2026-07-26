using System.ComponentModel.DataAnnotations;

namespace AppMorador.Application.PermissoesVeiculares;

public sealed class CriarPermissaoVeicularRequest
{
    [Required(ErrorMessage = "Ponto de acesso é obrigatório.")]
    public required Guid PontoAcessoId { get; set; }
}

public sealed class PermissaoVeicularResponse
{
    public required Guid Id { get; init; }

    public required Guid VeiculoId { get; init; }

    public required Guid PontoAcessoId { get; init; }

    public required string PontoAcessoNome { get; init; }
}
